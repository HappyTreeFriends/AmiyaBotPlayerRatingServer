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
            var gameId = gameManager.CreateNewGame();
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

        public async Task GetGame(string gameId)
        {
            var game = GameManager.GetGame(gameId);

            if (game == null||!game.PlayerList.ContainsKey(Context.ConnectionId))
            {
                await Clients.Caller.SendAsync("GameNotFound");
                return;
            }

            var playerlist = game.PlayerList.Select(x => new
            {
                UserId = x.Value,
                UserSignalRId = x.Key,
                UserName = _dbContext.Users.Find(x.Value)?.Nickname,
                UserEmail = _dbContext.Users.Find(x.Value)?.Email,
                GameId = gameId,
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
            game.PlayerList.Add(Context.ConnectionId,appUser.Id);

            var playerlist = game.PlayerList.Select(x => new
            {
                UserId = x.Value,
                UserSignalRId = x.Key,
                UserName = _dbContext.Users.Find(x.Value)?.Nickname,
                UserEmail = _dbContext.Users.Find(x.Value)?.Email,
                GameId = gameId,
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
            var game = GameManager.GetGame(gameId);
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
            var game = GameManager.GetGame(gameId);
            game.PlayerList.Remove(Context.ConnectionId);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
            await Clients.Group(gameId).SendAsync("PlayerLeft", JsonConvert.SerializeObject(new
            {
                LeavingMethod = "Left",
                LeavingPlayerSignalRId = Context.ConnectionId,
            }));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task StartGame(string gameId)
        {
            var playerlist = GameManager.GetGame(gameId).PlayerList.Select(x => new
            {
                UserId = x.Value,
                UserSignalRId = x.Key,
                UserName = _dbContext.Users.Find(x.Value)?.Nickname,
                UserEmail = _dbContext.Users.Find(x.Value)?.Email,
                GameId = gameId,
            });

            await Clients.Group(gameId).SendAsync("GameStart", JsonConvert.SerializeObject(new
            {
                GameId = gameId,
                PlayerList = playerlist
            }));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task SendMove(string gameId, string move)
        {
            var game = GameManager.GetGame(gameId);
            var manager = GameManager.GetGameManager(game.GameType);
            var ret = manager.HandleMove(game, Context.ConnectionId, move);

            await Clients.Group(gameId).SendAsync("ReceiveMove", Context.ConnectionId, ret);
        }
    }
}
