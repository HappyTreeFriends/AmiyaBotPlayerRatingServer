using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Linq;
using static AmiyaBotPlayerRatingServer.GameLogic.IGameManager;

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

                //随机选择5个维度有值的维度
                var randomProperties = Properties.Where(r=>GetPropValue(randomOperator, r)!=null).OrderBy(x => random.Next()).Take(5).ToList();
                if (randomProperties.Count < 5)
                {
                    continue;
                }
                foreach (var property in randomProperties)
                {
                    question.CharacterProperties[property] = GetPropValue(randomOperator, property)!;
                    question.CharacterPropertiesUsed[property] = true;
                }

                foreach(var property in Properties)
                {
                    if (!question.CharacterProperties.ContainsKey(property))
                    {
                        question.CharacterProperties[property] = "";
                        question.CharacterPropertiesUsed[property] = false;
                    }
                    question.CharacterPropertiesRevived[property] = false;
                }

                //题目出好了

                game.QuestionList.Add(question);
            }

            return game;
        }

        private String? GetPropValue(JObject op, String prop)
        {
            var retValue = "";
            switch (prop)
            {
                case "稀有度":
                    retValue= op["rarity"]?.ToString() ?? "";
                    break;
                case "职业":
                    retValue = op["classes"]?.ToString()??""; break;
                case "子职业":
                    retValue = op["classes_sub"]?.ToString()??""; break;
                case "种族":
                    retValue = op["race"]?.ToString() ?? ""; break;
                case "势力":
                    retValue = op["nation"]?.ToString() ?? ""; break;
                case "性别":
                    retValue = op["sex"]?.ToString()??""; break;
                case "队伍":
                    retValue = op["team"]?.ToString() ?? ""; break;
                case "阵营":
                    retValue = op["group"]?.ToString() ?? ""; break;
                case "画师":
                    retValue = op["drawer"]?.ToString() ?? ""; break;
            }

            if (String.IsNullOrWhiteSpace(retValue) || retValue == "未知")
            {
                return null;
            }

            return retValue;
        }

        public Task<Game?> CreateNewGame(Dictionary<string, JToken> param)
        {
            return Task.FromResult(GenerateGame());
        }

        private bool IsOperatorName(string name)
        {
            var charDataJson = memoryCache.GetJson("character_names.json")?.ToObject<Dictionary<String, String>>();
            if (charDataJson == null)
            {
                //ERROR
                return false;
            }

            return charDataJson.Values.Contains(name);
        }

        public Task<object> HandleMove(Game rawGame, string playerId, string move)
        {
            var game = rawGame as CypherChallengeGame;
            var moveObj = JObject.Parse(move);

            if (game == null || moveObj == null || moveObj["CharacterName"] == null)
            {
                throw new DataException("Move Payload不合法");
            }

            var characterName = moveObj["CharacterName"]!.ToString();

            if (game.IsStarted == false)
            {
                return Task.FromResult<object>(new
                {
                    Result = "NotStarted",
                    PlayerId = playerId,
                    CharacterName = characterName,
                    Completed = game.IsCompleted,
                    CompleteTime = game.CompleteTime
                });
            }

            if (!IsOperatorName(characterName))
            {
                game.PlayerMoveList.Add(new PlayerMove()
                {
                    PlayerId = playerId,
                    CharacterName = characterName,
                    IsCorrect = false,
                    IsValid = false,
                });

                return Task.FromResult<object>(new
                {
                    Result = "NotOperator",
                    PlayerId = playerId,
                    CharacterName = characterName,
                    Completed = game.IsCompleted,
                    CompleteTime = game.CompleteTime
                });
            }

            //是干员，检测是否已经回答过
            var currentQuestion = game.QuestionList[game.CurrentQuestionIndex];

            var existingAnswer = currentQuestion.AnswerList.FirstOrDefault(a => a.CharacterName == characterName);

            if (existingAnswer!=null)
            {
                game.PlayerMoveList.Add(new PlayerMove()
                {
                    PlayerId = playerId,
                    CharacterName = characterName,
                    IsCorrect = false,
                    IsValid = true,
                });

                return Task.FromResult<object>(new
                {
                    Result = "Answered",
                    PlayerId = playerId,
                    CharacterName = characterName,
                    Completed = game.IsCompleted,
                    CompleteTime = game.CompleteTime
                });
            }

            //接下来分两种情况,回答正确和回答错误
            //不管哪一种，最后都要返回完整Game

            if (currentQuestion.CharacterName == characterName)
            {
                //回答正确
                //首先记录回答
                var answer = new CypherChallengeGame.Answer()
                {
                    CharacterName = characterName,
                    CharacterId = currentQuestion.CharacterId,
                    AnswerTime = DateTime.Now,
                    IsAnswerCorrect = true,
                    PlayerId = playerId,
                    CharacterProperties = currentQuestion.CharacterProperties, //返回该干员的完整Property
                    CharacterPropertiesResult = currentQuestion.CharacterProperties.ToDictionary(k=>k.Key,k=>"Correct")
                };

                currentQuestion.AnswerList.Add(answer);
                currentQuestion.CharacterPropertiesRevived = currentQuestion.CharacterPropertiesUsed;

                //更新分数
                if (game.PlayerScore.ContainsKey(playerId))
                {
                    game.PlayerScore[playerId] += 100;
                }
                else
                {
                    game.PlayerScore.TryAdd(playerId, 100);
                }

                currentQuestion.IsCompleted = true;
                game.CurrentQuestionIndex++;

                //添加PlayerMove
                game.PlayerMoveList.Add(
                    new PlayerMove()
                    {
                        PlayerId = playerId,
                        CharacterName = characterName,
                        IsCorrect = true,
                        IsValid = true,
                    });
            }
            else
            {
                //回答错误,给出提示

                var operatorNames = memoryCache.GetObject<Dictionary<String, String>>("character_names.json");
                var operatorList = memoryCache.GetJson("operator_archive.json");

                var thisOperatorId = operatorNames.Where(k => k.Value == characterName).Select(k=>k.Key).FirstOrDefault();
                if (thisOperatorId == null)
                {
                    //TODO这里需要LogError因为数据出错
                    return Task.FromResult<object>(new
                    {
                        Result = "NotOperator",
                        PlayerId = playerId,
                        CharacterName = characterName,
                        Completed = game.IsCompleted,
                        CompleteTime = game.CompleteTime
                    });
                }

                var thisOperator = operatorList[thisOperatorId] as JObject;

                var thisOperatorsProperty = new Dictionary<String, String>();
                foreach (var prop in currentQuestion.CharacterPropertiesUsed)
                {
                    if (prop.Value)
                    {
                        thisOperatorsProperty[prop.Key] = GetPropValue(thisOperator, prop.Key);
                    }
                    
                }

                var verifiedProperties = currentQuestion.CharacterPropertiesUsed.Where(v => v.Value).Select(v => v.Key)
                    .Where(k => currentQuestion.CharacterProperties[k] == thisOperatorsProperty[k]).ToDictionary(k => k, k => thisOperatorsProperty[k]);

                var answer = new CypherChallengeGame.Answer()
                {
                    CharacterName = characterName,
                    CharacterId = currentQuestion.CharacterId,
                    AnswerTime = DateTime.Now,
                    IsAnswerCorrect = true,
                    PlayerId = playerId,
                    //返回该干员的Property中，和正确答案一样的部分
                    CharacterProperties = verifiedProperties,
                    CharacterPropertiesResult = currentQuestion.CharacterPropertiesUsed.ToDictionary(k => k.Key, k => verifiedProperties[k.Key]!=null ? "Correct" : "Wrong")
                };

                currentQuestion.AnswerList.Add(answer);
                //检测并更新 currentQuestion.CharacterPropertyRevived
                foreach (var property in verifiedProperties)
                {
                    if (currentQuestion.CharacterProperties[property.Key] == property.Value)
                    {
                        currentQuestion.CharacterPropertiesRevived[property.Key] = true;
                    }
                }

                currentQuestion.GuessChanceLeft--;
                if(currentQuestion.GuessChanceLeft==0)
                {
                    //回答错误，且没有机会了
                    currentQuestion.IsCompleted = false;
                    game.CurrentQuestionIndex++;
                }

                //添加PlayerMove
                game.PlayerMoveList.Add(
                    new PlayerMove()
                    {
                        PlayerId = playerId,
                        CharacterName = characterName,
                        IsCorrect = false,
                        IsValid = true,
                    });
            }

            if (game.IsCompleted != true)
            {
                if (game.QuestionList.All(q => q.IsCompleted)||game.CurrentQuestionIndex>game.QuestionList.Count)
                {
                    game.IsCompleted = true;
                    game.CompleteTime = DateTime.Now;

                    //CreateStatistics(game);
                }
            }
            
            return Task.FromResult<object>(new
            {
                Result = currentQuestion.IsCompleted?"Correct":"Wrong",
                PlayerId = playerId,
                CharacterName = characterName,
                CurrentQuestionIndex = game.CurrentQuestionIndex,
                CurrentQuestionIndexHint = "注意，在最后一题结束时，CurrentQuestionIndex可能会超出QuestionList的长度。",
                Completed = game.IsCompleted,
                CompleteTime = game.CompleteTime,
                Game = FormatGame(game)
            });
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
                    if (index > game.CurrentQuestionIndex)
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
                        CharacterProperties = answer.CharacterProperties.Where(k => answer.CharacterPropertiesResult.ContainsKey(k.Key) || game.IsCompleted || question.IsCompleted).ToDictionary(),
                        CharacterPropertiesResult = answer.CharacterPropertiesResult,
                    });
                }

                questionList.Add(new
                {
                    GuessChanceLeft = question.GuessChanceLeft,
                    IsCompleted = question.IsCompleted,
                    CharacterName = (game.IsCompleted||question.IsCompleted)?question.CharacterName:"",
                    CharacterId = (game.IsCompleted || question.IsCompleted) ? question.CharacterId : "",
                    CharacterProperties = question.CharacterProperties.Where(k => question.CharacterPropertiesRevived[k.Key]|| game.IsCompleted || question.IsCompleted ).ToDictionary(),
                    CharacterPropertiesRevived = question.CharacterPropertiesRevived,
                    CharacterPropertiesUsed = question.CharacterPropertiesUsed,
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

        public Task<object> GetCloseGamePayload(Game rawGame)
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
            //每一题只能提示一次
            var cypherGame = game as CypherChallengeGame;
            var currentQuestion = cypherGame!.QuestionList[cypherGame.CurrentQuestionIndex];

            if (currentQuestion.IsHinted)
            {
                return Task.FromResult(new RequestHintOrGiveUpResult()
                {
                    GiveUpTriggered = false,
                    HintTriggered = false,
                });
            }

            //随机选择一个未揭露的属性
            var unrevealedProperties = currentQuestion.CharacterPropertiesUsed.FirstOrDefault(k => !currentQuestion.CharacterPropertiesRevived[k.Key]);
            if (unrevealedProperties.Key == null)
            {
                return Task.FromResult(new RequestHintOrGiveUpResult()
                {
                    GiveUpTriggered = false,
                    HintTriggered = false,
                });
            }

            //标记为已揭露
            currentQuestion.CharacterPropertiesRevived[unrevealedProperties.Key] = true;

            return Task.FromResult(new RequestHintOrGiveUpResult()
            {
                Payload = new
                {
                    Property = unrevealedProperties.Key, 
                    Value = currentQuestion.CharacterProperties[unrevealedProperties.Key],
                    Game = FormatGame(cypherGame),
                },
                GiveUpTriggered = false,
                HintTriggered = false,
            });
        }

    }
}
