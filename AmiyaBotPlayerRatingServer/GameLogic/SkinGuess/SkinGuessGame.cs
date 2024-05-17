using System.Collections.Concurrent;

namespace AmiyaBotPlayerRatingServer.GameLogic.SkinGuess
{
    public class SkinGuessGame:Game
    {
        public class Answer
        {
            public String CharacterName { get; set; }
            public String CharacterId { get; set; }

            public String SkinName { get; set; }
            public String SkinId { get; set; }

            public String ImageUrl { get; set; }
            public int RandomNumber { get; set; }

            public String? ImageBase64 { get; set; } = "Not Used In Current Version";
            public String? HintImageBase64 { get; set; } = "Not Used In Current Version";

            public bool Completed { get; set; }
            public DateTime AnswerTime { get; set; }
            public string? PlayerId { get; set; }
        }

        public List<Answer> AnswerList { get; set; }

        public ConcurrentDictionary<String, double> PlayerScore { get; set; } = new ConcurrentDictionary<string, double>();
    }
}
