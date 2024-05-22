using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;
using AmiyaBotPlayerRatingServer.GameLogic.SkinGuess;
using AmiyaBotPlayerRatingServer.RealtimeHubs;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;
using static AmiyaBotPlayerRatingServer.GameLogic.Game;

namespace AmiyaBotPlayerRatingServer.GameLogic
{
    public class SystemNotification
    {
        public string Id { get; set; }
        public string Message { get; set; }
        public DateTime ExpiredAt { get; set; }
    }

    public abstract class GameManager
    {
        public static readonly List<Game> GameList = new List<Game>();
        private static readonly Task cleanTask = Task.CompletedTask;
        public static readonly List<SystemNotification> Notifications = new List<SystemNotification>();

        static GameManager()
        {
            cleanTask = cleanTask.ContinueWith(async (_) =>
            {
                while (true)
                {
                    await Task.Delay(1000 * 60 * 5);
                    var completedTimeout = GameManager.GameList.Where(x => x.IsClosed==false && x.IsCompleted && (
                        DateTime.Now - x.CompleteTime > new TimeSpan(0, 1, 0, 0)));
                    
                    var startedTimeout = GameManager.GameList.Where(x => x.IsStarted && (
                        DateTime.Now - x.StartTime > new TimeSpan(1, 0, 0, 0)));

                    var allTimeout = completedTimeout.Concat(startedTimeout).Distinct().ToList();

                    foreach (var game in allTimeout)
                    {
                        game.IsClosed = true;
                        game.CloseTime = DateTime.Now;
                    }
                }
            });
        }
        
        public static Game? GetGameByJoinCode(string joinCode)
        {
            return GameList.Find(x => x.JoinCode==joinCode&&x.IsClosed==false);
        }

        public static Game? GetGame(string id)
        {
            return GameList.Find(x => x.Id == id);
        }

        public static async Task<String> RequestJoinCode()
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
            } while (GameManager.GameList.Any(g => g.JoinCode == joinCode&&g.IsClosed==false));

            return joinCode;
        }

        public static async Task RallyPoint(Game game, int playerId, string rallyName, int timeout)
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
                        rallyNode.IsCompleted= true;
                        // DI IHubContext<GameHub>
                        //var hubContext = GlobalHost.ConnectionManager.GetHubContext()
                        //hubContext.Clients.Group(groupName).sendMessage(message);
                    }
                }
            }

        }

        public abstract Task<Game> CreateNewGame(Dictionary<String, JToken> param);

        public abstract Task<object> HandleMove(Game game, string playerId, string move);

        public abstract Task<object> GetGamePayload(Game game);
        public abstract Task<object> GetGameStartPayload(Game game);
        public abstract Task<object> GetCloseGamePayload(Game game);

        public abstract Task<double> GetScore(Game game, string player);

    }
}
