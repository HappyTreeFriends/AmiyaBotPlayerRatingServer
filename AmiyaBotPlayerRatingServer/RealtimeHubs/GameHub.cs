using AmiyaBotPlayerRatingServer.GameLogic;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System.Security.Claims;
using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.Model;
using AmiyaBotPlayerRatingServer.Utility;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace AmiyaBotPlayerRatingServer.RealtimeHubs
{
    public class GameHub : Hub
    {
        private readonly PlayerRatingDatabaseContext _dbContext;
        private readonly GameManager _gameManager;
        private readonly IDatabase _redisService;


        public GameHub(PlayerRatingDatabaseContext dbContext,
            IConnectionMultiplexer redisService, GameManager gameManager)
        {
            _dbContext = dbContext;
            _gameManager = gameManager;
            _redisService = redisService.GetDatabase();
        }

        #region Helper Methods

        private async Task<ApplicationUser> ValidateUser()
        {
            var appUser = await _dbContext.Users.FindAsync(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (appUser == null)
            {
                //登录信息失效，此时就是要弹回，所以抛出异常
                throw new UnauthorizedAccessException();
            }

            return appUser;
        }

        private async Task<Game> ValidateGame(string gameId, bool readOnly = true)
        {
            var game = await _gameManager.GetGameAsync(gameId, readOnly);

            if (game == null)
            {
                //获取了错误的房间信息，此时就是要弹回，所以抛出异常
                throw new UnauthorizedAccessException();
            }

            return game;
        }

        private Task<IGameManager> ValidateManager(String gameType)
        {
            var manager = _gameManager.CreateGameManager(gameType);

            if (manager == null)
            {
                //获取了错误的房间信息，此时就是要弹回，所以抛出异常
                throw new UnauthorizedAccessException();
            }

            return Task.FromResult(manager);
        }
        
        private async Task<Object> FormatPlayerList(Game game)
        {
            var manager = _gameManager.CreateGameManager(game.GameType);
            return await Task.WhenAll(game.PlayerList.Select(async x =>
            {
                var user = await _dbContext.Users.FindAsync(x.Key);
                return new
                {
                    UserId = x.Key,
                    UserSignalRId = x.Value,
                    UserName = user?.Nickname,
                    UserAvatar = user?.Avatar,
                    Score = await manager.GetScore(game, x.Key)
                };
            }));
        }

        #endregion

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [UsedImplicitly]
        public async Task Me()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                //登录信息失效，此时就是要弹回，所以抛出异常
                throw new UnauthorizedAccessException();
            }

            var myGames = await _gameManager.GetGameByCreatorIdAsync(userId);

            await Clients.Caller.SendAsync("MyConnectionInfo", JsonConvert.SerializeObject(new
            {
                ConnectionId = Context.ConnectionId,
                Id = userId,
                CreatedGames = myGames.Select(x => x.Id),
            }));
        }


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [UsedImplicitly]
        public async Task GetServerTime()
        {
            await Clients.Caller.SendAsync("ServerTime", JsonConvert.SerializeObject(new
            {
                UtcNow = DateTime.UtcNow,
                LocalNow = DateTime.Now
            }));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [UsedImplicitly]
        public async Task GetGame(string id)
        {
            await using var game = await ValidateGame(id,false);
            var manager = await ValidateManager(game.GameType);
            var appUser = await ValidateUser();

            if (!game.PlayerList.ContainsKey(appUser.Id))
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "您需要先加入该房间才能查看房间信息。",
                }));
                return;
            }

            game.PlayerList[appUser.Id] = Context.ConnectionId;
            await _gameManager.SaveGameAsync(game);

            await Groups.AddToGroupAsync(Context.ConnectionId, game.Id!);
            await Clients.Caller.SendAsync("GameInfo", JsonConvert.SerializeObject(new
            {
                Game = await manager.GetGamePayload(game),
                PlayerList = await FormatPlayerList(game),
                Payload = await manager.GetGamePayload(game),
            }));
            
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [UsedImplicitly]
        public async Task CreateGame(string gameType,string param)
        {
            var gameManager = await ValidateManager(gameType);
            var appUser = await ValidateUser();

            //用户每10秒才能创建一个房间
            var lastUserTime = _redisService.StringGet("AmiyaBot-Minigame-CreateGameLock-" + appUser.Id);
            if(lastUserTime.HasValue && DateTime.UtcNow - Convert.ToDateTime(lastUserTime) < TimeSpan.FromSeconds(10))
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "您创建房间的速度过快，请稍候再试。",
                }));
                return;
            }
            _redisService.StringSet("AmiyaBot-Minigame-CreateGameLock-" + appUser.Id, DateTime.UtcNow.ToString("s"), TimeSpan.FromSeconds(15));

            var paramObj = JsonConvert.DeserializeObject<Dictionary<String,object>>(param);
            if (paramObj == null)
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "错误的房间设置s",
                }));
                return;
            }

            await using var game = await gameManager.CreateNewGame(paramObj);

            if (game == null)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "服务器过忙，请稍候再试。",
                }));
                return;
            }

            game.GameType = gameType;
            game.IsPrivate = paramObj.ContainsKey("IsPrivate") && paramObj["IsPrivate"].ToString() == "True";
            game.RoomSettings = paramObj;
            game.CreatorId= appUser.Id;
            game.CreatorConnectionId = Context.ConnectionId;
            game.CreateTime = DateTime.Now;
            game.PlayerList.TryAdd(appUser.Id, Context.ConnectionId);
            
            game.JoinCode= await _gameManager.RequestJoinCode();

            await _gameManager.SaveGameAsync(game);
            
            await Groups.AddToGroupAsync(Context.ConnectionId, game.Id!);
            await Clients.Group(game.Id!).SendAsync("GameCreated", JsonConvert.SerializeObject(new
            {
                CreatorId = appUser.Id,
                CreatorConnectionId = Context.ConnectionId,
                CreatorName = appUser.Nickname,

                GameId = game.Id,
                GameType = gameType,
                GameJoinCode = game.JoinCode,

                Game = await gameManager.GetGamePayload(game),
            }));
        }


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [UsedImplicitly]
        public async Task JoinGame(string joinCode)
        {
            var appUser = await ValidateUser();

            await using var game = await _gameManager.GetGameByJoinCodeAsync(joinCode,false);
            if (game == null)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "该房间不存在",
                }));
                return;
            }

            var manager = await ValidateManager(game.GameType);

            //看一下是不是已经在游戏里了
            if (game.PlayerList.ContainsKey(appUser.Id))
            {
                //替换身份
                game.PlayerList[appUser.Id] = Context.ConnectionId;
            }
            else
            {
                game.PlayerList.TryAdd(appUser.Id, Context.ConnectionId);
            }
            
            //如果是房主，更新房主的ConnectionId
            if (game.CreatorId == appUser.Id)
            {
                game.CreatorConnectionId = Context.ConnectionId;
            }

            await _gameManager.SaveGameAsync(game);

            await Groups.AddToGroupAsync(Context.ConnectionId, game.Id!);
            await Clients.Group(game.Id!).SendAsync("PlayerJoined", JsonConvert.SerializeObject(new
            {
                UserId = appUser.Id,
                UserSignalRId = Context.ConnectionId,
                UserName = appUser.Nickname,
                UserEmail = appUser.Email,
                GameId = game.Id,

                PlayerList = await FormatPlayerList(game),
                JoinedPlayer = new
                {
                    Id = appUser.Id,
                    ConnectionId = Context.ConnectionId,
                    Nickname = appUser.Nickname
                },
                Game = await manager.GetGamePayload(game),
            }) );
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [UsedImplicitly]
        public async Task KickPlayer(string gameId, string playerId)
        {
            await using var game = await ValidateGame(gameId,false);
            var appUser = await ValidateUser();

            if (game.CreatorId != appUser.Id)
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "您不是房主，无法踢人",
                }));
                return;
            }

            var manager = await ValidateManager(game.GameType);

            var oldConnectionId = game.PlayerList[playerId];
            game.PlayerList.Remove(playerId);

            var playerKickedResponse = JsonConvert.SerializeObject(new
            {
                LeavingMethod = "Kicked",
                LeavingPlayerId = playerId,
                LeavingPlayerConnectionId = oldConnectionId,
                LeavingPlayer = new
                {
                    Id = playerId,
                    ConnectionId = oldConnectionId,
                },
                Game = await manager.GetGamePayload(game),
            });

            await _gameManager.SaveGameAsync(game);
            await Clients.Client(playerId).SendAsync("PlayerKicked", playerKickedResponse);
            await Clients.Group(gameId).SendAsync("PlayerKicked", playerKickedResponse);
            await Groups.RemoveFromGroupAsync(playerId, gameId);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [UsedImplicitly]
        public async Task LeaveGame(string gameId)
        {
            await using var game = await ValidateGame(gameId,false);
            var appUser = await ValidateUser();

            if (!game.PlayerList.ContainsKey(appUser.Id))
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "您需要先加入该房间才能查看房间信息。",
                }));
                return;
            }

            var manager = await ValidateManager(game.GameType);

            game.PlayerList.Remove(appUser.Id);

            await _gameManager.SaveGameAsync(game);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
            await Clients.Group(gameId).SendAsync("PlayerLeft", JsonConvert.SerializeObject(new
            {
                LeavingMethod = "Left",
                LeavingPlayerConnectionId = Context.ConnectionId,
                LeavingPlayerId = appUser.Id,
                LeavingPlayer = new
                {
                    Id = appUser.Id,
                    ConnectionId = Context.ConnectionId,
                },
                Game = await manager.GetGamePayload(game),
            }));

            //如果房主离开了，就关闭房间
            if (game.CreatorId == appUser.Id)
            {
                await CloseGame(gameId);
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [UsedImplicitly]
        public async Task ChangeGameSettings(string gameId, string settings)
        {
            await using var game = await ValidateGame(gameId,false);
            var appUser = await ValidateUser();

            if (game.CreatorId != appUser.Id)
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "您不是房主，无法修改房间设置",
                }));
                return;
            }


            var manager = await ValidateManager(game.GameType);

            var settingsObj = JsonConvert.DeserializeObject<Dictionary<String,Object>>(settings);
            if (settingsObj == null)
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "错误的房间设置",
                }));
                return;
            }

            game.RoomSettings = settingsObj;

            //isPrivate要特别处理
            game.IsPrivate = settingsObj.ContainsKey("IsPrivate") && settingsObj["IsPrivate"].ToString() == "True";

            await _gameManager.SaveGameAsync(game);
            await Clients.Group(gameId).SendAsync("GameSettingsChanged", JsonConvert.SerializeObject(new
            {
                Game = await manager.GetGamePayload(game),
                Settings = settingsObj,
            }));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [UsedImplicitly]
        public async Task CloseGame(string gameId)
        {
            await using var game = await ValidateGame(gameId,false);
            var appUser = await ValidateUser();
            var manager = await ValidateManager(game.GameType);

            if (game.CreatorId != appUser.Id)
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "您不是房主，无法关闭房间",
                }));
                return;
            }

            var oldCompleteState = game.IsCompleted;
            if (game.IsClosed == false)
            {
                game.IsClosed = true;
                game.CloseTime = DateTime.Now;
            }

            var payload = await manager.GetCloseGamePayload(game);
            var ret = JsonConvert.SerializeObject(new
            {
                Game = await manager.GetGamePayload(game),
                Payload = payload,
            });

            if (game.IsCompleted && oldCompleteState==false)
            {
                await Clients.Group(gameId).SendAsync("GameCompleted", ret);
            }

            await _gameManager.SaveGameAsync(game);
            await Clients.Group(gameId).SendAsync("GameClosed", ret);
        }

        //放弃整个游戏
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [UsedImplicitly]
        public async Task CompleteGame(string gameId)
        {
            await using var game = await ValidateGame(gameId, false);
            var appUser = await ValidateUser();
            var manager = await ValidateManager(game.GameType);

            if (game.CreatorId != appUser.Id)
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "您不是房主，无法关闭房间",
                }));
                return;
            }

            if (game.IsClosed)
            {
                return;
            }

            var payload = await manager.GetCompleteGamePayload(game);
            var ret = JsonConvert.SerializeObject(new
            {
                Game = await manager.GetGamePayload(game),
                Payload = payload,
            });

            await Clients.Group(gameId).SendAsync("GameCompleted", ret);
            await _gameManager.SaveGameAsync(game);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [UsedImplicitly]
        public async Task StartGame(string gameId)
        {
            await using var game = await ValidateGame(gameId,false);
            var manager = await ValidateManager(game.GameType);
            var appUser = await ValidateUser();
            
            if (game.CreatorId != appUser.Id)
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "您不是房主，无法开始游戏",
                }));
                return;
            }

            var payload = await manager.GetGameStartPayload(game);

            game.IsStarted = true;
            game.StartTime = DateTime.Now;
            
            await _gameManager.SaveGameAsync(game);

            await Clients.Group(gameId).SendAsync("GameStarted", JsonConvert.SerializeObject(new
            {
                GameId = gameId,
                PlayerList = FormatPlayerList(game),
                Payload = payload,
                Game = await manager.GetGamePayload(game),
            }));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [UsedImplicitly]
        public async Task RallyPointCreate(string gameId, string rallyData)
        {
            await using var game = await ValidateGame(gameId,false);
            var appUser = await ValidateUser();
            var manager = await ValidateManager(game.GameType);

            if (!game.PlayerList.ContainsKey(appUser.Id))
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "您不是该房间内的玩家。",
                }));
                return;
            }

            var rallyDataObj = JsonConvert.DeserializeObject<Dictionary<String,JToken>>(rallyData);
            if (rallyDataObj == null)
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "错误的集结点参数。",
                }));
                return;
            }

            var rallyName = rallyDataObj.GetValueOrDefault("Name")?.ToString();

            if (rallyName == null)
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "未提供集结点名称。",
                }));
                return;
            }

            var rallyNode = game.RallyNodes.GetValueOrSetDefault(rallyName, new Game.RallyNode(rallyName));

            await _gameManager.SaveGameAsync(game);

            await Clients.Caller.SendAsync("RallyPointCreated", JsonConvert.SerializeObject(new
            {
                Name = rallyName,
                CreatePlayer = appUser.Id,
                Players = rallyNode.PlayerIds,
                Game = manager.GetGamePayload(game)
            }));

            await Clients.Group(gameId).SendAsync("RallyPointStatus", JsonConvert.SerializeObject(new
            {
                Name = rallyName,
                Players = rallyNode.PlayerIds,
                Game = manager.GetGamePayload(game)
            }));
        }


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [UsedImplicitly]
        public async Task RallyPointStatus(string gameId, string rallyData)
        {
            await using var game = await ValidateGame(gameId,false);
            var appUser = await ValidateUser();
            var manager = await ValidateManager(game.GameType);

            if (!game.PlayerList.ContainsKey(appUser.Id))
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "您不是该房间内的玩家。",
                }));
                return;
            }

            var rallyDataObj = JsonConvert.DeserializeObject<Dictionary<String,JToken>>(rallyData);
            if (rallyDataObj == null)
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "错误的集结点参数。",
                }));
                return;
            }

            var rallyName = rallyDataObj.GetValueOrDefault("Name")?.ToString();

            if (rallyName == null)
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "未提供集结点名称。",
                }));
                return;
            }

            var rallyNode = game.RallyNodes.GetValueOrSetDefault(rallyName, new Game.RallyNode(rallyName));

            await Clients.Caller.SendAsync("RallyPointStatus", JsonConvert.SerializeObject(new
            {
                Name = rallyName,
                Players = rallyNode.PlayerIds,
                Game = manager.GetGamePayload(game),
            }));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [UsedImplicitly]
        public async Task RallyPointReached(string gameId,string rallyData)
        {
            await using var game = await ValidateGame(gameId,false);
            var appUser = await ValidateUser();
            var manager = await ValidateManager(game.GameType);

            if (!game.PlayerList.ContainsKey(appUser.Id))
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "您不是该房间内的玩家。",
                }));
                return;
            }

            var rallyDataObj = JsonConvert.DeserializeObject<Dictionary<String,JToken>>(rallyData);
            if (rallyDataObj == null)
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "错误的集结点参数。",
                }));
                return;
            }

            var rallyName = rallyDataObj.GetValueOrDefault("Name")?.ToString();

            if (rallyName == null)
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "未提供集结点名称。",
                }));
                return;
            }

            var rallyNode = game.RallyNodes.GetValueOrSetDefault(rallyName, new Game.RallyNode(rallyName));

            rallyNode.PlayerIds.Add(appUser.Id);
            
            await Clients.Group(gameId).SendAsync("RallyPointStatus", JsonConvert.SerializeObject(new
            {
                Name = rallyName,
                Players = rallyNode.PlayerIds,
                Game = manager.GetGamePayload(game),
            }));
            

            //如果所有玩家都到达了这个点，就触发事件
            if (rallyNode.PlayerIds.Count == game.PlayerList.Count&& game.PlayerList.All(p=>rallyNode.PlayerIds.Contains(p.Key)) )
            {
                await Clients.Group(gameId).SendAsync("RallyPointReached", JsonConvert.SerializeObject(new
                {
                    Name = rallyName,
                    Players = rallyNode.PlayerIds,
                    Game = manager.GetGamePayload(game),
                }));

                //game.RallyNodes.Remove(rallyName, out _);
            }

            await _gameManager.SaveGameAsync(game);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [UsedImplicitly]
        public async Task SendMove(string gameId, string move)
        {
            await using var game = await ValidateGame(gameId,false);
            var manager = await ValidateManager(game.GameType);
            var appUser = await ValidateUser();

            if (!game.PlayerList.ContainsKey(appUser.Id))
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "您需要先加入该房间才能查看房间信息。",
                }));
                return;
            }

            if (game.IsCompleted)
            {
                return;
            }

            var oldCompleteState = game.IsCompleted;

            var ret = await manager.HandleMove(game, appUser.Id, move);

            await _gameManager.SaveGameAsync(game);

            var response = JsonConvert.SerializeObject(new
            {
                Payload = ret,
                Game = await manager.GetGamePayload(game),
                PlayerList = await FormatPlayerList(game),
            });

            await Clients.Group(gameId).SendAsync("ReceiveMove", response);

            if (game.IsCompleted && oldCompleteState == false)
            {
                await Clients.Group(gameId).SendAsync("GameCompleted", response);
            }
        }

        //请求给出提示(可能导致该题被放弃)
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [UsedImplicitly]
        public async Task RequestHint(string gameId)
        {
            await using var game = await ValidateGame(gameId,false);
            var manager = await ValidateManager(game.GameType);
            var appUser = await ValidateUser();

            if (!game.PlayerList.ContainsKey(appUser.Id))
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "您需要先加入该房间才能查看房间信息。",
                }));
                return;
            }

            if (game.IsCompleted)
            {
                return;
            }

            var oldCompleteState = game.IsCompleted;

            var hintResult = await manager.RequestHint(game, appUser.Id);
            
            if (hintResult.GiveUpTriggered)
            { 
                await Clients.Group(gameId).SendAsync("GiveUp", JsonConvert.SerializeObject(new 
                {
                    PlayerId = appUser.Id,
                    GameId = gameId,
                    Payload = hintResult.Payload,
                    Game = await manager.GetGamePayload(game),
                    PlayerList = await FormatPlayerList(game),
                }));
            
            }
            else if(hintResult.HintTriggered)
            {
                await Clients.Group(gameId).SendAsync("Hint", JsonConvert.SerializeObject(new
                {
                    PlayerId = appUser.Id,
                    Payload = hintResult.Payload,
                    Game = await manager.GetGamePayload(game),
                    PlayerList = await FormatPlayerList(game),
                }));
            }


            if (game.IsCompleted && oldCompleteState == false)
            {
                await Clients.Group(gameId).SendAsync("GameCompleted", JsonConvert.SerializeObject(new
                {
                    Payload = hintResult.Payload,
                    Game = await manager.GetGamePayload(game),
                    PlayerList = await FormatPlayerList(game),
                }));
            }

            await _gameManager.SaveGameAsync(game);
        }

        //放弃一小题
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [UsedImplicitly]
        public async Task GiveUp(string gameId)
        {
            await using var game = await ValidateGame(gameId,false);
            var manager = await ValidateManager(game.GameType);
            var appUser = await ValidateUser();

            if (!game.PlayerList.ContainsKey(appUser.Id))
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "您需要先加入该房间才能查看房间信息。",
                }));
                return;
            }

            if (game.IsCompleted)
            {
                return;
            }

            var oldCompleteState = game.IsCompleted;

            var giveUpResult = await manager.GiveUp(game, appUser.Id);

            if (giveUpResult.GiveUpTriggered)
            {
                await Clients.Group(gameId).SendAsync("GiveUp", JsonConvert.SerializeObject(new
                {
                    PlayerId = appUser.Id,
                    GameId = gameId,
                    Payload = giveUpResult.Payload,
                    Game = await manager.GetGamePayload(game),
                    PlayerList = await FormatPlayerList(game),
                }));
            }

            if (game.IsCompleted && oldCompleteState == false)
            {
                var response = JsonConvert.SerializeObject(new
                {
                    Payload = giveUpResult.Payload,
                    Game = await manager.GetGamePayload(game),
                    PlayerList = await FormatPlayerList(game),
                });

                await Clients.Group(gameId).SendAsync("GameCompleted", response);
            }

            await _gameManager.SaveGameAsync(game);
        }
        
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [UsedImplicitly]
        public async Task GetNotification()
        {
            var validNodes = _dbContext.SystemNotifications.Where(n => n.ExpiredAt >= DateTime.UtcNow);
            foreach (var notification in validNodes)
            {
                if (notification.ExpiredAt > DateTime.Now)
                {
                    await Clients.Caller.SendAsync("SystemNotification", JsonConvert.SerializeObject(new
                    {
                        Id = notification.Id,
                        Message = notification.Message,
                        ExpiredAt = notification.ExpiredAt,
                    }));
                }
            }
        }
        
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [UsedImplicitly]
        public async Task Chat(string gameId, string message)
        {
            await using var game = await ValidateGame(gameId);
            var appUser = await ValidateUser();

            if (!game.PlayerList.ContainsKey(appUser.Id))
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "您需要先加入该房间才能查看房间信息。",
                }));
                return;
            }

            await Clients.Group(gameId).SendAsync("Chat", JsonConvert.SerializeObject(new
            {
                UserId = appUser.Id,
                UserName = appUser.Nickname,
                Message = message,
                GameId = gameId,
            }));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [UsedImplicitly]
        public async Task Rematch(string gameId)
        {
            await using var game = await ValidateGame(gameId);
            var manager = await ValidateManager(game.GameType);
            var appUser = await ValidateUser();

            if (game.CreatorId != appUser.Id)
            {
                await Clients.Caller.SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "您不是房主，无法重新开始游戏",
                }));
                return;
            }

            //重新创建一个游戏,然后给所有人发送重新开始的消息
            await this.CreateGame(game.GameType, JsonConvert.SerializeObject(game.RoomSettings));
        }
    }
}
