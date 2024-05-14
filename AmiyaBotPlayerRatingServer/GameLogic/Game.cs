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
        public DateTime CreateTime { get; set; }

        public bool IsPrivate { get; set; }
        public String JoinPassword { get; set; }

        public bool IsStarted { get; set; }
        public DateTime StartTime { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CompleteTime { get; set; }

        public Dictionary<String, String> PlayerList { get; set; } = new Dictionary<String,String>();
    }
}
