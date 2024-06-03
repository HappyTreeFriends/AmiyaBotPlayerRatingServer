using System.Collections.Concurrent;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
#pragma warning disable CS8618
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
            public String CharacterId { get; set; }
            public String SkillName { get; set; }
            public String SkillId { get; set; }
            public List<GridPoint> GridPointList { get; set; }
            public bool Completed { get; set; }
            public DateTime AnswerTime { get; set; }
            public string? PlayerId { get; set; }
        }
        
        public List<List<String>> Grid { get; set; }
        public int GridWidth { get; set; }
        public int GridHeight { get; set; }

        public List<GridAnswer> AnswerList { get; set; }

        public ConcurrentDictionary<String,double> PlayerScore { get; set; } = new();

        public class PlayerMove
        {
            public string PlayerId { get; set; }
            public String CharacterName { get; set; }
            public bool IsOperator { get; set; }
            public bool IsCorrect { get; set; }
        }

        public List<PlayerMove> PlayerMoveList { get; set; } = new();
    }
}
