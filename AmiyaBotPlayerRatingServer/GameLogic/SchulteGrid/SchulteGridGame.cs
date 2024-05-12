using System.Collections.Concurrent;

namespace AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid
{
    public class SchulteGridGame: Game
    {
        public class GridPoint
        { 
            public int X { get; set; }
            public int Y { get; set; }
        }

        public class GridAnswer
        {
            public String CharacterName { get; set; }
            public String SkillName { get; set; }
            public List<GridPoint> GridPointList { get; set; }
            public bool Completed { get; set; }
            public DateTime AnswerTime { get; set; }
            public string? PlayerId { get; set; }
        }
        
        public List<List<String>> Grid { get; set; }
        public int GridWidth { get; set; }
        public int GridHeight { get; set; }

        public List<GridAnswer> AnswerList { get; set; }

        public ConcurrentDictionary<String,double> PlayerScore { get; set; } = new ConcurrentDictionary<string, double>();
    }
}
