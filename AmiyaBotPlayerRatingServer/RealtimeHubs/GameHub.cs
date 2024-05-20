using AmiyaBotPlayerRatingServer.Controllers.Game.SchulteGrid;
using AmiyaBotPlayerRatingServer.GameLogic;
using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;
using Microsoft.AspNetCore.SignalR;
using System.Security.AccessControl;
using Newtonsoft.Json;
using System.Security.Claims;
using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using AmiyaBotPlayerRatingServer.Utility;

namespace AmiyaBotPlayerRatingServer.RealtimeHubs
{
    public class GameHub : Hub
    {
        private readonly PlayerRatingDatabaseContext _dbContext;
        private readonly GameManagerFactory _gameManagerFactory;


        public GameHub(PlayerRatingDatabaseContext dbContext,GameManagerFactory gameManagerFactory)
        {
            _dbContext = dbContext;
            _gameManagerFactory = gameManagerFactory;
        }

        #region Helper Methods

        private async Task<ApplicationUser> ValidateUser()
        {
            var appUser = await _dbContext.Users.FindAsync(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (appUser == null)
            {
                throw new UnauthorizedAccessException();
            }

            return appUser;
        }

        private Task<Game> ValidateGame(string gameId)
        {
            var game = GameManager.GetGame(gameId);

            if (game == null)
            {
                throw new UnauthorizedAccessException();
            }

            return Task.FromResult(game);
        }

        private Task<GameManager> ValidateManager(String gameType)
        {
            var manager = _gameManagerFactory.CreateGameManager(gameType);

            if (manager == null)
            {
                throw new UnauthorizedAccessException();
            }

            return Task.FromResult(manager);
        }

        private async Task<Tuple<Game, GameManager, ApplicationUser>> Validate(string gameId)
        {
            var appUser = await _dbContext.Users.FindAsync(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (appUser == null)
            {
                throw new UnauthorizedAccessException();
            }

            var game = GameManager.GetGame(gameId);

            if (game == null)
            {
                throw new UnauthorizedAccessException();
            }

            if (!game.PlayerList.ContainsKey(appUser.Id))
            {
                throw new UnauthorizedAccessException();
            }

            var manager = _gameManagerFactory.CreateGameManager(game.GameType);

            if (manager == null)
            {
                throw new UnauthorizedAccessException();
            }

            return Tuple.Create(game, manager, appUser);
        }

        private Object FormatPlayerList(Game game)
        {
            var manager = _gameManagerFactory.CreateGameManager(game.GameType);
            return game.PlayerList.Select(x =>
            {
                var user = _dbContext.Users.Find(x.Key);
                return new
                {
                    UserId = x.Key,
                    UserSignalRId = x.Value,
                    UserName = user?.Nickname,
                    UserAvatar = user?.Avatar,
                    Score = manager?.GetScore(game, x.Key)
                };
            });
        }

        #endregion

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task Me()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            await Clients.Caller.SendAsync("MyConnectionInfo", JsonConvert.SerializeObject(new
            {
                ConnectionId = Context.ConnectionId,
                Id = userId,
                CreatedGames = GameManager.GameList.Where(x => x.CreatorId == userId).Select(x => x.Id),
            }));
        }


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task GetServerTime()
        {
            await Clients.Caller.SendAsync("ServerTime", JsonConvert.SerializeObject(new
            {
                UtcNow = DateTime.UtcNow,
                LocalNow = DateTime.Now
            }));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task GetGame(string id)
        {
            var (game, manager, appUser) = await Validate(id);

            if (game.PlayerList.ContainsKey(appUser.Id))
            {
                game.PlayerList[appUser.Id] = Context.ConnectionId;
                await Groups.AddToGroupAsync(Context.ConnectionId, game.Id);
            }
            await Clients.Caller.SendAsync("GameInfo", JsonConvert.SerializeObject(new
            {
                GameId = game.Id,
                GameType = game.GameType,
                GameJoinCode = game.JoinCode,
                GameStarted = game.IsStarted,
                GameStartTime = game.StartTime,
                GameCompleted = game.IsCompleted,
                GameCompleteTime = game.CompleteTime,
                GameClosed = game.IsClosed,
                GameCloseTime = game.CloseTime,
                CreatorId = game.CreatorId,
                CreatorConnectionId = game.CreatorConnectionId,
                PlayerList = FormatPlayerList(game),
                CurrentStatus = manager.GetGameStatus(game),
            }));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task CreateGame(string gameType,string param)
        {
            var appUser = await _dbContext.Users.FindAsync(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (appUser == null)
            {
                throw new UnauthorizedAccessException();
            }

            var gameManager = _gameManagerFactory.CreateGameManager(gameType);
            if (gameManager == null)
            {
                throw new UnauthorizedAccessException();
            }

            var game = await gameManager.CreateNewGame(param);
            
            game.Id = Guid.NewGuid().ToString();
            game.CreatorId= appUser.Id;
            game.CreatorConnectionId = Context.ConnectionId;
            game.CreateTime = DateTime.Now;
            game.PlayerList.TryAdd(appUser.Id, Context.ConnectionId);
            
            game.JoinCode= GameManager.RequestJoinCode();
            GameManager.GameList.Add(game);
            
            await Groups.AddToGroupAsync(Context.ConnectionId, game.Id);
            await Clients.Group(game.Id).SendAsync("GameCreated", JsonConvert.SerializeObject(new
            {
                CreatorId = appUser.Id,
                CreatorConnectionId = Context.ConnectionId,
                CreatorName = appUser.Nickname,

                GameId = game.Id,
                GameType = gameType,
                GameJoinCode = game.JoinCode,
            }));
        }


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task JoinGame(string joinCode)
        {
            var appUser = await _dbContext.Users.FindAsync(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (appUser == null)
            {
                throw new UnauthorizedAccessException();
            }
            
            var game = GameManager.GetGameByJoinCode(joinCode);
            if (game == null)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("Alert", JsonConvert.SerializeObject(new
                {
                    Message = "该房间不存在",
                }));
                return;
            }
            var manager = _gameManagerFactory.CreateGameManager(game.GameType);

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

            await Groups.AddToGroupAsync(Context.ConnectionId, game.Id);
            await Clients.Group(game.Id).SendAsync("PlayerJoined", JsonConvert.SerializeObject(new
            {
                UserId = appUser.Id,
                UserSignalRId = Context.ConnectionId,
                UserName = appUser.Nickname,
                UserEmail = appUser.Email,
                GameId = game.Id,
                PlayerList = FormatPlayerList(game),
            }) );
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task KickPlayer(string gameId, string playerId)
        {
            var (game, manager, appUser) = await Validate(gameId);

            if (game.CreatorId != appUser.Id)
            {
                throw new UnauthorizedAccessException();
            }

            if (!game.PlayerList.ContainsKey(playerId))
            {
                return;
            }
            var oldConnectionId = game.PlayerList[playerId];
            game.PlayerList.TryRemove(playerId,out _);

            var playerKickedResponse = JsonConvert.SerializeObject(new
            {
                LeavingMethod = "Kicked",
                LeavingPlayerId = playerId,
                LeavingPlayerConnectionId = oldConnectionId,
            });

            await Clients.Client(playerId).SendAsync("PlayerKicked", playerKickedResponse);
            await Clients.Group(gameId).SendAsync("PlayerKicked", playerKickedResponse);
            await Groups.RemoveFromGroupAsync(playerId, gameId);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task LeaveGame(string gameId)
        {
            var (game, manager, appUser) = await Validate(gameId);

            game.PlayerList.TryRemove(appUser.Id,out _);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
            await Clients.Group(gameId).SendAsync("PlayerLeft", JsonConvert.SerializeObject(new
            {
                LeavingMethod = "Left",
                LeavingPlayerConnectionId = Context.ConnectionId,
                LeavingPlayerId = appUser.Id,
            }));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task CloseGame(string gameId)
        {
            var (game, manager, appUser) = await Validate(gameId);

            if (game.CreatorId != appUser.Id)
            {
                throw new UnauthorizedAccessException();
            }


            if (game.IsClosed == false)
            {
                game.IsClosed = true;
                game.CloseTime = DateTime.Now;
            }

            var ret = manager.CloseGame(game);

            await Clients.Group(gameId).SendAsync("GameClosed", ret);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task StartGame(string gameId)
        {
            var (game, manager, appUser)= await Validate(gameId);

            if (game.CreatorId != appUser.Id)
            {
                throw new UnauthorizedAccessException();
            }

            await manager.GameStart(game);

            game.IsStarted = true;
            game.StartTime = DateTime.Now;
            
            await Clients.Group(gameId).SendAsync("GameStarted", JsonConvert.SerializeObject(new
            {
                GameId = gameId,
                PlayerList = FormatPlayerList(game)
            }));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task RallyPoint(string gameId,string rallyData)
        {
            var (game, manager, appUser) = await Validate(gameId);
            
            

            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task SendMove(string gameId, string move)
        {
            var (game, manager, appUser) = await Validate(gameId);

            if (game.IsCompleted)
            {
                return;
            }

            var ret = manager.HandleMove(game, appUser.Id, move);

            await Clients.Group(gameId).SendAsync("ReceiveMove", ret);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task GetNotification()
        {
            foreach (var notification in GameManager.Notifications)
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
        public async Task Chat(string gameId, string message)
        {
            var (game, manager, appUser) = await Validate(gameId);

            await Clients.Group(gameId).SendAsync("Chat", JsonConvert.SerializeObject(new
            {
                UserId = appUser.Id,
                UserName = appUser.Nickname,
                Message = message,
            }));
        }
    }
}
