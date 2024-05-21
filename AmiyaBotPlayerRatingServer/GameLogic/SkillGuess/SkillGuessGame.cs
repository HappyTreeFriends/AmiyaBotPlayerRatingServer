using System.Collections.Concurrent;

namespace AmiyaBotPlayerRatingServer.GameLogic.SkillGuess
{
    public class SkillGuessGame:Game
    {
        public class Answer
        {
            public String CharacterName { get; set; }
            public String CharacterId { get; set; }

            public String SkillName { get; set; }
            public String SkillId { get; set; }

            public String ImageUrl { get; set; }

            public bool Completed { get; set; }
            public DateTime AnswerTime { get; set; }
            public string? PlayerId { get; set; }
        }

        public int CurrentQuestionIndex { get; set; } = 0;

        public List<Answer> AnswerList { get; set; }

        public ConcurrentDictionary<String, double> PlayerScore { get; set; } = new ConcurrentDictionary<string, double>();
    }
}
