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

namespace AmiyaBotPlayerRatingServer.RealtimeHubs
{
    public class GameHub : Hub
    {
        private readonly PlayerRatingDatabaseContext _dbContext;

        public GameHub(PlayerRatingDatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        private async Task<Tuple<Game, GameManager, ApplicationUser>> Validate(string gameId)
        {
            var appUser = await _dbContext.Users.FindAsync(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (appUser == null)
            {
                throw new UnauthorizedAccessException();
            }

            var game = GameManager.GetGame(gameId);

            if (game == null || !game.PlayerList.ContainsKey(Context.ConnectionId))
            {
                throw new UnauthorizedAccessException();
            }

            var manager = GameManager.GetGameManager(game.GameType);

            if (manager == null)
            {
                throw new UnauthorizedAccessException();
            }

            return Tuple.Create(game, manager, appUser);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task GetMyId()
        {
            await Clients.Caller.SendAsync("ReceiveMyId", JsonConvert.SerializeObject(new
            {
                SignalRId = Context.ConnectionId,
                UserId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            }));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task CreateGame(string gameType)
        {
            var appUser = await _dbContext.Users.FindAsync(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (appUser == null)
            {
                throw new UnauthorizedAccessException();
            }

            var gameManager = GameManager.GetGameManager(gameType);
            var gameId = await gameManager.CreateNewGame();
            var game = GameManager.GetGame(gameId);
            game.CreatorId=Context.ConnectionId;
            game.PlayerList.Add(Context.ConnectionId, appUser.Id);
            
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
            await Clients.Group(gameId).SendAsync("GameCreated", JsonConvert.SerializeObject(new
            {
                CreatorSignalRId = Context.ConnectionId,
                CreatorId = appUser.Id,
                CreatorName = appUser.Nickname,
                GameType = gameType,
                GameId = gameId
            }));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task GetGame(string gameId)
        {
            var (game, manager, _) = await Validate(gameId);

            var playerlist = game.PlayerList.Select(x => new
            {
                UserId = x.Value,
                UserSignalRId = x.Key,
                UserName = _dbContext.Users.Find(x.Value)?.Nickname,
                UserEmail = _dbContext.Users.Find(x.Value)?.Email,
                GameId = gameId,
                Score = manager.GetScore(game, x.Key)
            });

            await Clients.Caller.SendAsync("GameInfo", JsonConvert.SerializeObject(new
            {
                GameId = gameId,
                CreatorSignalRId = game.CreatorId,
                PlayerList = playerlist
            }));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task JoinGame(string gameId)
        {
            var appUser = await _dbContext.Users.FindAsync(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (appUser == null)
            {
                throw new UnauthorizedAccessException();
            }
            
            var game = GameManager.GetGame(gameId);
            var manager = GameManager.GetGameManager(game.GameType);

            //看一下是不是已经在游戏里了
            if (game.PlayerList.ContainsValue(appUser.Id))
            {
                //替换身份
                var oldConnectionId = game.PlayerList.FirstOrDefault(x => x.Value == appUser.Id).Key;
                game.PlayerList.Remove(oldConnectionId);
                game.PlayerList.Add(Context.ConnectionId, appUser.Id);

                if (game.CreatorId == oldConnectionId)
                {
                    game.CreatorId = Context.ConnectionId;
                }
            }
            else
            {
                game.PlayerList.Add(Context.ConnectionId, appUser.Id);
            }

            var playerlist = game.PlayerList.Select(x => new
            {
                UserId = x.Value,
                UserSignalRId = x.Key,
                UserName = _dbContext.Users.Find(x.Value)?.Nickname,
                UserEmail = _dbContext.Users.Find(x.Value)?.Email,
                GameId = gameId,
                Score = manager.GetScore(game, x.Key)
            });

            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
            await Clients.Group(gameId).SendAsync("PlayerJoined", JsonConvert.SerializeObject(new
            {
                UserId = appUser.Id,
                UserSignalRId = Context.ConnectionId,
                UserName = appUser.Nickname,
                UserEmail = appUser.Email,
                GameId = gameId,
                PlayerList = playerlist
            }) );
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task KickPlayer(string gameId, string playerId)
        {
            var (game, manager, _) = await Validate(gameId);

            if (game.CreatorId != Context.ConnectionId)
            {
                throw new UnauthorizedAccessException();
            }

            game.PlayerList.Remove(playerId);

            var playerKickedResponse = JsonConvert.SerializeObject(new
            {
                LeavingMethod = "Kicked",
                LeavingPlayerSignalRId = playerId,
            });

            await Clients.Client(playerId).SendAsync("PlayerKicked", playerKickedResponse);
            await Clients.Group(gameId).SendAsync("PlayerKicked", playerKickedResponse);
            await Groups.RemoveFromGroupAsync(playerId, gameId);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task LeaveGame(string gameId)
        {
            var (game, manager, _) = await Validate(gameId);

            game.PlayerList.Remove(Context.ConnectionId);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
            await Clients.Group(gameId).SendAsync("PlayerLeft", JsonConvert.SerializeObject(new
            {
                LeavingMethod = "Left",
                LeavingPlayerSignalRId = Context.ConnectionId,
            }));
        }

        public async Task CloseGame(string gameId)
        {
            var (game, manager, _) = await Validate(gameId);

            if (game.CreatorId != Context.ConnectionId)
            {
                throw new UnauthorizedAccessException();
            }
            
            await Clients.Group(gameId).SendAsync("GameClosed", JsonConvert.SerializeObject(new
            {
                GameId = gameId,
            }));

            foreach (var player in game.PlayerList)   
            {
                await Groups.RemoveFromGroupAsync(player.Key, gameId);
            }

            GameManager.GameList.Remove(game);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task StartGame(string gameId)
        {
            var (game, manager, _) = await Validate(gameId);

            var playerlist = game.PlayerList.Select(x => new
            {
                UserId = x.Value,
                UserSignalRId = x.Key,
                UserName = _dbContext.Users.Find(x.Value)?.Nickname,
                UserEmail = _dbContext.Users.Find(x.Value)?.Email,
                GameId = gameId,
                Score = manager.GetScore(game, x.Key)
            });

            await Clients.Group(gameId).SendAsync("GameStarted", JsonConvert.SerializeObject(new
            {
                GameId = gameId,
                PlayerList = playerlist
            }));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task SendMove(string gameId, string move)
        {
            var (game, manager, _) = await Validate(gameId);

            var ret = manager.HandleMove(game, Context.ConnectionId, move);

            await Clients.Group(gameId).SendAsync("ReceiveMove", Context.ConnectionId, ret);
        }
    }
}
