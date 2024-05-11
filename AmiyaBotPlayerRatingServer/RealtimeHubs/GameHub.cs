using AmiyaBotPlayerRatingServer.Controllers.Game.SchulteGrid;
using AmiyaBotPlayerRatingServer.GameLogic;
using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;
using Microsoft.AspNetCore.SignalR;
using System.Security.AccessControl;

namespace AmiyaBotPlayerRatingServer.RealtimeHubs
{
    public class GameHub : Hub
    {

        public async Task CreateGame(string gameType)
        {
            var gameManager = GameManager.GetGameManager(gameType);
            var gameId = gameManager.CreateNewGame();
            

            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
            await Clients.Group(gameId).SendAsync("Send", $"{Context.ConnectionId} has created the game {gameId}");
        }

        public async Task JoinGame(string gameId)
        {
            var game = GameManager.GetGame(gameId);
            game.PlayerList.Add(Context.ConnectionId);

            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
            await Clients.Group(gameId).SendAsync("Send", $"{Context.ConnectionId} has joined the game {gameId}");
        }

        public async Task KickPlayer(string gameId, string playerId)
        {
            var game = GameManager.GetGame(gameId);
            game.PlayerList.Remove(playerId);

            await Clients.Client(playerId).SendAsync("Send", $"You have been kicked from the game {gameId}");
            await Clients.Group(gameId).SendAsync("Send", $"{playerId} has been kicked from the game {gameId}");
            await Groups.RemoveFromGroupAsync(playerId, gameId);
        }

        public async Task LeaveGame(string gameId)
        {
            var game = GameManager.GetGame(gameId);
            game.PlayerList.Remove(Context.ConnectionId);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
            await Clients.Group(gameId).SendAsync("Send", $"{Context.ConnectionId} has left the game {gameId}");
        }

        public async Task StartGame(string gameId)
        {
            await Clients.Group(gameId).SendAsync("Send", $"Game {gameId} has started");
        }

        public async Task SendMove(string gameId, string move)
        {
            var game = GameManager.GetGame(gameId);
            var manager = GameManager.GetGameManager(game.GameType);
            var ret = manager.HandleMove(game, Context.ConnectionId, move);

            await Clients.Group(gameId).SendAsync("ReceiveMove", Context.ConnectionId, ret);
        }
    }
}
