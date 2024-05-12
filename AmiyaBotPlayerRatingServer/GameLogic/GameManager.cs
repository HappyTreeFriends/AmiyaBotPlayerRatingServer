using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;

namespace AmiyaBotPlayerRatingServer.GameLogic
{
    public abstract class GameManager
    {
        public static List<Game> GameList = new List<Game>();

        public static GameManager? GetGameManager(string gameType)
        {
            if (gameType == "SchulteGrid")
            {
                return new SchulteGridGameManager();
            }
            else
            {
                return null;
            }
        }


        public static Game? GetGameByJoinCode(string joinCode)
        {
            return GameList.Find(x => x.JoinCode==joinCode);
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
            } while (GameManager.GameList.Any(g => g.JoinCode == joinCode));

            return joinCode;
        }

        public abstract Task<Game> CreateNewGame(string param);
        public abstract Task GameStart(Game game);
        public abstract string HandleMove(Game game, string contextConnectionId, string move);

        public abstract object GetGameStatus(Game game);
        public abstract double GetScore(Game game, string player);

    }
}
