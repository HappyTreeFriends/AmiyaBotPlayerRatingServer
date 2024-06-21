using System.Collections.Concurrent;
using JetBrains.Annotations;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
#pragma warning disable CS8618
namespace AmiyaBotPlayerRatingServer.GameLogic.SkinGuess
{
    public class SkinGuessGame:Game,IScorable, ISequentialQuestionGame<SkinGuessGame.Question>
    {
        public class Question
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

            public int HintLevel { get; set; }
        }

        public int CurrentQuestionIndex { get; set; }
        public int MaxQuestionCount { get; set; }

        public List<Question> QuestionList { get; set; }

        public Dictionary<String, double> PlayerScore { get; set; } = new ();

        public List<PlayerMove> PlayerMoveList { get; set; } = new();
    }
}
