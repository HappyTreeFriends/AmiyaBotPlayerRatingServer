using AmiyaBotPlayerRatingServer.Model;

namespace AmiyaBotPlayerRatingServer.GameLogic
{
    public class Game
    {
        public String Id { get; set; }
        public String JoinCode { get; set; }
        
        public String GameType { get; set; }

        public String CreatorId { get; set; }
        public String CreatorConnectionId { get; set; }

        public Dictionary<String, String> PlayerList { get; set; } = new Dictionary<String,String>();
        public bool Started { get; set; }
    }
}
