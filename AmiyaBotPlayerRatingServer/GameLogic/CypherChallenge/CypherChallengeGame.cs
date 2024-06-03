using System.Collections.Concurrent;

namespace AmiyaBotPlayerRatingServer.GameLogic.CypherChallenge
{
    public class CypherChallengeGame:Game
    {
        public class Answer
        {
            public String CharacterName { get; set; }
            public String CharacterId { get; set; }
            
            public DateTime AnswerTime { get; set; }
            public string? PlayerId { get; set; }

            //势力
            public String NationResult { get; set; }
            //职业
            public String ProfessionResult { get; set; }
            //子职业
            public String SubProfessionResult { get; set; }
            //稀有度
            public String RarityResult { get; set; }
            //性别
            public String GenderResult { get; set; }
            //队伍
            public String TeamResult { get; set; }
            //阵营
            public String GroupResult { get; set; }
            //画师
            public String IllustResult { get; set; }


            public bool IsCharacterCorrect { get; set; }
        }

        public int GuessChanceLeft { get; set; } = 10;
        
        public String CharacterName { get; set; }
        public String CharacterId { get; set; }

        public String NationAnswer { get; set; }
        public String ProfessionAnswer { get; set; }
        public String SubProfessionAnswer { get; set; }
        public String RarityAnswer { get; set; }
        public String GenderAnswer { get; set; }
        public String TeamAnswer { get; set; }
        public String GroupAnswer { get; set; }
        public String IllustAnswer { get; set; }

        public bool IsNationRevealed { get; set; }
        public bool IsProfessionRevealed { get; set; }
        public bool IsSubProfessionRevealed { get; set; }
        public bool IsRarityRevealed { get; set; }
        public bool IsGenderRevealed { get; set; }
        public bool IsTeamRevealed { get; set; }
        public bool IsGroupRevealed { get; set; }
        public bool IsIllustRevealed { get; set; }


        public List<Answer> AnswerList { get; set; }

        public Dictionary<String, double> PlayerScore { get; set; } = new();


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
