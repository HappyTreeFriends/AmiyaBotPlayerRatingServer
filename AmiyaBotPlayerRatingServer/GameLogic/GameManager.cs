﻿using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;
using AmiyaBotPlayerRatingServer.GameLogic.SkinGuess;
using Newtonsoft.Json.Linq;

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

        public static String RequestJoinCode()
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

        public abstract Task<Game> CreateNewGame(Dictionary<String, JToken> param);
        public abstract Task GameStart(Game game);
        public abstract string HandleMove(Game game, string playerId, string move);
        public abstract string CloseGame(Game game);

        public abstract object GetGameStatus(Game game);
        public abstract double GetScore(Game game, string player);

    }
}
