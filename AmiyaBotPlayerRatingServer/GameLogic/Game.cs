using AmiyaBotPlayerRatingServer.Model;

namespace AmiyaBotPlayerRatingServer.GameLogic
{
    public class Game
    {
        public String GameId { get; set; }
        public String GameType { get; set; }
        public String CreatorId { get; set; }
        public Dictionary<String, String> PlayerList { get; set; } = new Dictionary<String,String>();
    }
}
