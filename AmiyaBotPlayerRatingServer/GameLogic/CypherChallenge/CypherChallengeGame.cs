using System.Collections.Concurrent;

namespace AmiyaBotPlayerRatingServer.GameLogic.CypherChallenge
{
    public class CypherChallengeGame:Game,IScorable, ISequentialQuestionGame<CypherChallengeGame.Question>
    {

        public class Answer
        {
            public String CharacterName { get; set; }
            public String CharacterId { get; set; }

            public Dictionary<String, String> CharacterProperties { get; set; } = new();
            public Dictionary<String, String> CharacterPropertiesResult { get; set; } = new();

            public DateTime AnswerTime { get; set; }
            public string? PlayerId { get; set; }
            
            public bool IsAnswerCorrect { get; set; }
        }

        public class Question
        {

            public int GuessChanceLeft { get; set; } = 10;

            public String CharacterName { get; set; }
            public String CharacterId { get; set; }

            public bool IsHinted { get; set; }
            public bool IsCompleted { get; set; }

            public Dictionary<String,String> CharacterProperties { get; set; } = new();

            public Dictionary<String, bool> CharacterPropertiesRevealed { get; set; } = new();
            public Dictionary<String, bool> CharacterPropertiesUsed { get; set; } = new();

            public List<Answer> AnswerList { get; set; } = new();
        }

        public int CurrentQuestionIndex { get; set; } = 0;

        public List<Question> QuestionList { get; set; } = new();
        public Dictionary<String, double> PlayerScore { get; set; } = new();

        public List<PlayerMove> PlayerMoveList { get; set; } = new();
    }
}
