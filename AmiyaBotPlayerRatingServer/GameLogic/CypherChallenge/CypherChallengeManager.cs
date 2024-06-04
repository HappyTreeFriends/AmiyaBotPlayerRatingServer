using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;
using Newtonsoft.Json.Linq;

namespace AmiyaBotPlayerRatingServer.GameLogic.CypherChallenge
{
    public class CypherChallengeManager(ArknightsMemoryCache memoryCache, PlayerRatingDatabaseContext dbContext)
        : IGameManager
    {
        private readonly List<String> Properties = new (){ "势力", "职业", "子职业", "稀有度", "性别", "队伍", "阵营", "画师" };
        
        private Game? GenerateGame()
        {
            var operatorNames = memoryCache.GetObject<Dictionary<String,String>>("character_names.json");
            var operatorList = memoryCache.GetJson("operator_archive.json");

            if (operatorNames == null || operatorList == null)
            {
                return null;
            }

            //随机选择一个
            var random = new Random();
            var operatorIdList = operatorNames.Keys.ToList();

            var game= new CypherChallengeGame();
            game.GameType="CypherChallenge";

            while (game.QuestionList.Count<8)
            {
                var randomIndex = random.Next(0, operatorIdList.Count);
                var operatorId = operatorIdList[randomIndex];
                var randomOperator = operatorList[operatorId] as JObject;

                if (randomOperator == null)
                {
                    continue;
                }

                var question = new CypherChallengeGame.Question()
                {
                    CharacterName = randomOperator["name"]?.ToString()??"",
                    CharacterId = operatorId,
                };

                //随机选择5个维度
                var randomProperties = Properties.OrderBy(x => random.Next()).Take(5).ToList();
                foreach (var property in randomProperties)
                {
                    question.CharacterProperties[property] = GetPropValue(randomOperator, property);
                    question.CharacterPropertyUsed[property] = true;
                }

                foreach(var property in Properties)
                {
                    if (!question.CharacterProperties.ContainsKey(property))
                    {
                        question.CharacterProperties[property] = "";
                        question.CharacterPropertyUsed[property] = false;
                    }
                    question.CharacterPropertyRevived[property] = false;
                }

                //题目出好了

                game.QuestionList.Add(question);
            }

            return game;
        }

        private String GetPropValue(JObject op, String prop)
        {
            return op[prop]?.ToString()??"";
        }

        public Task<Game?> CreateNewGame(Dictionary<string, JToken> param)
        {
            return Task.FromResult(GenerateGame());
        }

        public Task<object> HandleMove(Game game, string playerId, string move)
        {
            throw new NotImplementedException();
        }

        public async Task<object> GetGamePayload(Game rawGame)
        {
            await Task.CompletedTask;

            var game = rawGame as CypherChallengeGame;

            if (game == null)
            {
                return Task.FromResult<object>(new { });
            }

            return new
            {
                Game = FormatGame(game)
            };
        }

        public Task<object> GetGameStartPayload(Game game)
        {
            return Task.FromResult<object>(new { });
        }

        private object FormatGame(CypherChallengeGame game)
        {
            var questionList = new List<object>();
            for (var index = 0; index < game.QuestionList.Count; index++)
            {
                var question = game.QuestionList[index];
                if (!game.IsCompleted)
                {
                    //未结束游戏只显示当前题目和已经回答的题目
                    if (index != game.CurrentQuestionIndex || !question.IsCompleted)
                    {
                        continue;
                    }
                }

                var answerList = new List<object>();
                foreach (var answer in question.AnswerList)
                {
                    answerList.Add(new
                    {
                        CharacterName = answer.CharacterName,
                        CharacterId = answer.CharacterId,
                        AnswerTime = answer.AnswerTime,
                        PlayerId = answer.PlayerId,
                        IsAnswerCorrect = answer.IsAnswerCorrect,
                        CharacterProperties = answer.CharacterProperties.Where(k => answer.CharacterPropertyResult.ContainsKey(k.Key) || game.IsCompleted || question.IsCompleted).ToDictionary(),
                        CharacterPropertyResult = answer.CharacterPropertyResult,
                    });
                }

                questionList.Add(new
                {
                    GuessChanceLeft = question.GuessChanceLeft,
                    IsCompleted = question.IsCompleted,
                    CharacterName = (game.IsCompleted||question.IsCompleted)?question.CharacterName:"",
                    CharacterId = (game.IsCompleted || question.IsCompleted) ? question.CharacterId : "",
                    CharacterProperties = question.CharacterProperties.Where(k => question.CharacterPropertyRevived[k.Key]|| game.IsCompleted || question.IsCompleted ).ToDictionary(),
                    CharacterPropertyRevived = question.CharacterPropertyRevived,
                    CharacterPropertyUsed = question.CharacterPropertyUsed,
                    AnswerList = answerList,
                });
            }
            
            return new
            {
                QuestionList = questionList,
                CurrentQuestionIndex = game.CurrentQuestionIndex,

                IsCompleted = game.IsCompleted,
                CompleteTime = game.CompleteTime,
                IsClosed = true,
                CloseTime = DateTime.Now
            };

        }

        public Task<object> GetCompleteGamePayload(Game rawGame)
        {
            var game = rawGame as CypherChallengeGame;

            if (game == null)
            {
                return Task.FromResult<object>(new { });
            }
            
            if (!game.IsCompleted)
            {
                game.IsCompleted = true;
                game.CompleteTime = DateTime.Now;
                //CreateStatistics(game);
            }

            return Task.FromResult<object>(new
            {
                Game = FormatGame(game),
            });
        }

        public async Task<object> GetCloseGamePayload(Game rawGame)
        {
            var game = rawGame as CypherChallengeGame;

            if (game == null)
            {
                return Task.FromResult<object>(new { });
            }

            game.IsClosed=true;
            game.CloseTime=DateTime.Now;

            if (!game.IsCompleted)
            {
                game.IsCompleted = true;
                game.CompleteTime = DateTime.Now;
                //CreateStatistics(game);
            }

            return Task.FromResult<object>(new
            {
                Game = FormatGame(game),
            });
        }

        public async Task<double> GetScore(Game rawGame, string player)
        {
            await Task.CompletedTask;

            var game = rawGame as CypherChallengeGame;

            if (game!.PlayerScore.ContainsKey(player))
            {
                return game.PlayerScore[player];
            }

            return 0;
        }

        public Task<IGameManager.RequestHintOrGiveUpResult> GiveUp(Game game, string appUserId)
        {
            throw new NotImplementedException();
        }

        public Task<IGameManager.RequestHintOrGiveUpResult> RequestHint(Game game, string appUserId)
        {
            throw new NotImplementedException();
        }

    }
}
