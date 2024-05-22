using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;
using AmiyaBotPlayerRatingServer.GameLogic.SkillGuess;
using AmiyaBotPlayerRatingServer.GameLogic.SkinGuess;
using AmiyaBotPlayerRatingServer.RealtimeHubs;
using Microsoft.AspNetCore.SignalR;
using static AmiyaBotPlayerRatingServer.GameLogic.Game;

namespace AmiyaBotPlayerRatingServer.GameLogic
{
    public class GameManager
    {
        public readonly List<Game> GameList = new List<Game>();
        public readonly List<SystemNotification> Notifications = new List<SystemNotification>();

        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<GameHub> _gameHub;

        public GameManager(IServiceProvider serviceProvider, IHubContext<GameHub> gameHub)
        {
            _serviceProvider = serviceProvider;
            _gameHub = gameHub;
        }

        public IGameManager CreateGameManager(string gameType)
        {
            return gameType switch
            {
                "SchulteGrid" => _serviceProvider!.GetService<SchulteGridGameManager>()!,
                "SkinGuess" => _serviceProvider!.GetService<SkinGuessManager>()!,
                "SkillGuess" => _serviceProvider!.GetService<SkillGuessManager>()!,
                _ => throw new ArgumentException("Invalid game type"),
            };
        }


        public Game? GetGameByJoinCode(string joinCode)
        {
            return GameList.Find(x => x.JoinCode == joinCode && x.IsClosed == false);
        }

        public Game? GetGame(string id)
        {
            return GameList.Find(x => x.Id == id);
        }

        public async Task<String> RequestJoinCode()
        {
            int maxTry = 100;
            string joinCode;
            do
            {
                joinCode = new Random().Next(100000, 999999).ToString();
                maxTry--;
                if (maxTry == 0)
                {
                    return "";
                }
            } while (GameList.Any(g => g.JoinCode == joinCode && g.IsClosed == false));

            return joinCode;
        }

        public async Task RallyPoint(Game game, int playerId, string rallyName, int timeout)
        {
            if (!game.RallyNodes.ContainsKey(rallyName))
            {
                game.RallyNodes[rallyName] = new RallyNode(rallyName);
            }

            var rallyNode = game.RallyNodes[rallyName];
            rallyNode.AddPlayer(playerId);

            await Task.Delay(timeout);

            if (rallyNode.PlayerIds.Count == game.PlayerList.Count)
            {
                lock (rallyNode)
                {
                    if (!rallyNode.IsCompleted)
                    {
                        rallyNode.IsCompleted = true;
                        // DI IHubContext<GameHub>
                        //var hubContext = GlobalHost.ConnectionManager.GetHubContext()
                        //hubContext.Clients.Group(groupName).sendMessage(message);
                    }
                }
            }

        }

    }
}
