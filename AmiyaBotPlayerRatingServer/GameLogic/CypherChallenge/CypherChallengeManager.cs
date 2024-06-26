using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Linq;
using static AmiyaBotPlayerRatingServer.GameLogic.IGameManager;

namespace AmiyaBotPlayerRatingServer.GameLogic.CypherChallenge
{
    public class CypherChallengeManager(ArknightsMemoryCache memoryCache, GameManager manager)
        : IGameManager
    {
        private readonly List<String> _properties = ["势力", "职业", "子职业", "稀有度", "性别", "队伍", "阵营", "画师"];
        
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

            game.MaxQuestionCount = 3;
            while (game.QuestionList.Count< game.MaxQuestionCount)
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
                var randomProperties = _properties.Where(r=>GetPropValue(randomOperator, r)!=null).OrderBy(x => random.Next()).Take(6).ToList();
                if (randomProperties.Count < 6)
                {
                    continue;
                }

                foreach (var property in _properties)
                {
                    question.CharacterProperties[property] = GetPropValue(randomOperator, property)!;
                }
                foreach (var property in randomProperties)
                {
                    question.CharacterPropertiesUsed[property] = true;
                }

                foreach(var property in _properties)
                {
                    if (!question.CharacterProperties.ContainsKey(property))
                    {
                        question.CharacterProperties[property] = "";
                        question.CharacterPropertiesUsed[property] = false;
                    }
                    question.CharacterPropertiesRevealed[property] = false;
                }

                //上一题的答案是下一题的第一个项目
                if (game.QuestionList.Count>=1)
                {
                    question.GuessChanceLeft--;

                    var lastQuestion = game.QuestionList.Last();
                    var answers = new Dictionary<String, string>();
                    foreach (var usedProp in question.CharacterPropertiesUsed.Where(k=>k.Value).Select(k=>k.Key))
                    {
                        var lastOp = lastQuestion.CharacterProperties[usedProp];
                        var currOp = question.CharacterProperties[usedProp];
                        var answerJd = JudgeAnswer(usedProp, lastOp, currOp);
                        if(answerJd == "Correct")
                        {
                            question.CharacterPropertiesRevealed[usedProp] = true;
                            answers.Add(usedProp, answerJd);
                        }
                    }

                    var answer = new CypherChallengeGame.Answer()
                    {
                        CharacterName = lastQuestion.CharacterName,
                        CharacterId = lastQuestion.CharacterId,
                        AnswerTime = DateTime.Now,
                        IsAnswerCorrect = true,
                        PlayerId = null,
                        CharacterProperties = lastQuestion.CharacterProperties,
                        CharacterPropertiesResult = answers
                    };

                    question.AnswerList.Add(answer);
                }
                else
                {
                    //第一题给阿米娅
                }

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

        private string JudgeAnswer(string prop, string userValue, string answerValue)
        {
            var retValue = "";
            switch (prop)
            {
                case "稀有度":
                case "职业":
                case "子职业":
                case "种族":
                case "势力":
                case "性别":
                case "队伍":
                case "阵营":
                default:
                    retValue = answerValue == userValue?"Correct":"Wrong";
                    break;
            }

            return retValue;
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

                var canQuestionReveal = (game.IsCompleted || question.IsCompleted || index < game.CurrentQuestionIndex);



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
                        CharacterProperties = canQuestionReveal?answer.CharacterProperties:null,
                        CharacterPropertiesResult = answer.CharacterPropertiesResult,
                    });
                }

                questionList.Add(new
                {
                    GuessChanceLeft = question.GuessChanceLeft,
                    IsCompleted = question.IsCompleted,
                    CharacterName = canQuestionReveal ? question.CharacterName : "",
                    CharacterId = canQuestionReveal ? question.CharacterId : "",
                    CharacterProperties = question.CharacterProperties.Where(
                        k => question.CharacterPropertiesRevealed.GetValueOrDefault(k.Key)
                             || canQuestionReveal).ToDictionary(),
                    CharacterPropertiesRevealed = question.CharacterPropertiesRevealed,
                    CharacterPropertiesUsed = question.CharacterPropertiesUsed,
                    AnswerList = answerList,
                });
            }

            return new
            {
                game.Id,
                game.GameType,
                game.JoinCode,

                game.CreatorId,
                game.CreatorConnectionId,
                game.CreateTime,

                game.IsStarted,
                game.StartTime,

                game.IsCompleted,
                game.CompleteTime,

                game.IsClosed,
                game.CloseTime,

                game.RoomSettings,


                QuestionList = questionList,
                CurrentQuestionIndex = game.CurrentQuestionIndex,
                MaxQuestionCount = game.MaxQuestionCount,
            };

        }
        
        public Task<Game?> CreateNewGame(Dictionary<string, object> param)
        {
            return Task.FromResult(GenerateGame());
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
            bool moveToNextQuestion = false;
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
                currentQuestion.CharacterPropertiesRevealed = currentQuestion.CharacterPropertiesUsed;

                //更新分数
                if (game.PlayerScore.ContainsKey(playerId))
                {
                    game.PlayerScore[playerId] += 200;
                }
                else
                {
                    game.PlayerScore.TryAdd(playerId, 200);
                }

                currentQuestion.IsCompleted = true;
                game.CurrentQuestionIndex++;
                moveToNextQuestion = true;

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

                var verifiedResult = verifiedProperties.ToDictionary(v=>v.Key,v =>
                    JudgeAnswer(v.Key, v.Value, currentQuestion.CharacterProperties[v.Key]));

                //检测并更新 currentQuestion.CharacterPropertyRevealed
                foreach (var property in verifiedProperties)
                {
                    if (currentQuestion.CharacterProperties[property.Key] == property.Value)
                    {
                        if (currentQuestion.CharacterPropertiesRevealed[property.Key] == false)
                        {
                            if (game.PlayerScore.ContainsKey(playerId))
                            {
                                game.PlayerScore[playerId] += 50;
                            }
                            else
                            {
                                game.PlayerScore.TryAdd(playerId, 50);
                            }

                            currentQuestion.CharacterPropertiesRevealed[property.Key] = true;
                        }
                    }
                }

                var answer = new CypherChallengeGame.Answer()
                {
                    CharacterName = characterName,
                    CharacterId = thisOperatorId,
                    AnswerTime = DateTime.Now,
                    IsAnswerCorrect = true,
                    PlayerId = playerId,
                    CharacterProperties = thisOperatorsProperty,
                    //返回该干员的Property中，和正确答案一样的部分
                    CharacterPropertiesResult = verifiedResult,
                };

                currentQuestion.AnswerList.Add(answer);

                currentQuestion.GuessChanceLeft--;
                if(currentQuestion.GuessChanceLeft==0)
                {
                    //回答错误，且没有机会了
                    currentQuestion.IsCompleted = false;
                    game.CurrentQuestionIndex++;
                    moveToNextQuestion = true;
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

                    manager.CreateStatistics(game);
                }
            }

            if (game.CurrentQuestionIndex >= game.QuestionList.Count)
            {
                game.CurrentQuestionIndex = game.QuestionList.Count-1;
            }
            
            return Task.FromResult<object>(new
            {
                Result = currentQuestion.IsCompleted?"Correct":"Wrong",
                MoveTonextQuestion = moveToNextQuestion,
                PlayerId = playerId,
                CharacterName = characterName,
                CurrentQuestionIndex = game.CurrentQuestionIndex,
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
                return new { };
            }

            return FormatGame(game);
        }

        public Task<object> GetGameStartPayload(Game game)
        {
            return Task.FromResult<object>(new { });
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
                manager.CreateStatistics(game);
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
                manager.CreateStatistics(game);
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
            var unrevealedProperties = currentQuestion.CharacterPropertiesUsed.FirstOrDefault(k => !currentQuestion.CharacterPropertiesRevealed[k.Key]);
            if (unrevealedProperties.Key == null)
            {
                return Task.FromResult(new RequestHintOrGiveUpResult()
                {
                    GiveUpTriggered = false,
                    HintTriggered = false,
                });
            }

            //标记为已揭露
            currentQuestion.CharacterPropertiesRevealed[unrevealedProperties.Key] = true;

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
