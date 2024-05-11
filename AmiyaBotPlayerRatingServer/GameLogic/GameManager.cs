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

        public static Game GetGame(object gameId)
        {
            return GameList.Find(x => x.GameId == gameId);
        }

        public abstract String CreateNewGame();
        
        public abstract string HandleMove(Game game, string contextConnectionId, string move);
    }
}
