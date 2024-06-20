using System.Data;
using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.Model;
using Newtonsoft.Json.Linq;
using static AmiyaBotPlayerRatingServer.GameLogic.IGameManager;

namespace AmiyaBotPlayerRatingServer.GameLogic.SkinGuess
{
    public class SkinGuessManager(ArknightsMemoryCache arknightsMemoryCache, PlayerRatingDatabaseContext dbContext,GameManager manager)
        : IGameManager
    {
        private static int RandomNumberGenerator ()=> new Random().Next(0, 100000);

        private SkinGuessGame GenerateTestGame()
        {
            var game = new SkinGuessGame();

            //手动添加一些测试数据
            game.GameType = "SkinGuess";
            game.QuestionList =
            [
                new()
                {
                    CharacterName = "雷蛇",
                    CharacterId = "char_107_liskam",
                    SkinName = "春竜",
                    SkinId = "char_107_liskam@nian#2",
                    ImageUrl = "https://media.prts.wiki/3/3d/%E7%AB%8B%E7%BB%98_%E9%9B%B7%E8%9B%87_skin1.png",
                    RandomNumber = RandomNumberGenerator()
                },

                new()
                {
                    CharacterName = "霜叶",
                    CharacterId = "char_109_silent",
                    SkinName = "破晓",
                    SkinId = "char_109_silent@nian#2",
                    ImageUrl = "https://media.prts.wiki/8/8c/立绘_霜叶_skin1.png",
                    RandomNumber = RandomNumberGenerator()
                }

            ];

            return game;
        }

        private SkinGuessGame GenerateRealGame()
        {
            var game = new SkinGuessGame();

            //手动添加一些测试数据
            game.GameType = "SkinGuess";
            game.QuestionList= new List<SkinGuessGame.Question>();

            var charMaps = arknightsMemoryCache.GetJson("character_names.json")?.ToObject<Dictionary<String,String>>();
            var skinUrls = arknightsMemoryCache.GetJson("skinUrls.json") as JObject;

            if (charMaps == null || skinUrls == null)
            {
                //ERROR
                return GenerateTestGame();
            }
            
            var operatorIdList = charMaps.Keys.ToList();
            var max = operatorIdList.Count;
            var random = new Random();

            while(game.QuestionList.Count<16)
            {
                var rand = random.Next(0, max);
                var charId = operatorIdList[rand];
                if (game.QuestionList.Any(x => x.CharacterId == charId))
                {
                    continue;
                }

                var charName = charMaps[charId];
                var skinList = (skinUrls[charId] as JObject)?.Properties().Select(p=>p.Value).ToList();

                if (skinList==null||skinList.Count <= 1)
                {
                    continue;
                }

                //randomly select a skin except the first one
                var randSkin = random.Next(1, skinList.Count);
                var skinUrl = skinList[randSkin].ToString();
                
                var answer = new SkinGuessGame.Question()
                {
                    CharacterName = charName,
                    CharacterId = charId,
                    SkinName = "",
                    SkinId = "",
                    ImageUrl = skinUrl,
                    RandomNumber = RandomNumberGenerator()
                };
                game.QuestionList.Add(answer);
            }

            return game;
        }

        private bool IsOperatorName(string name)
        {
            var charDataJson = arknightsMemoryCache.GetJson("character_names.json")?.ToObject<Dictionary<String,String>>();
            if (charDataJson == null)
            {
                //ERROR
                return false;
            }

            return charDataJson.Values.Contains(name);
        }

        private object FormatGame(SkinGuessGame game)
        {
            var questionList = game.QuestionList.Select(x => new
            {
                CharacterName = x.CharacterName,
                CharacterId = x.CharacterId,
                SkinName = x.SkinName,
                SkinId = x.SkinId,
                ImageUrl = x.ImageUrl,
                Completed = x.Completed,
                AnswerTime = x.AnswerTime,
                PlayerId = x.PlayerId
            }).ToList();

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
            };
        }

        public Task<Game?> CreateNewGame(Dictionary<String, object> param)
        {
            var game = GenerateRealGame();
            return Task.FromResult<Game?>(game);
        }

        public async Task<object> GetGamePayload(Game rawGame)
        {
            await Task.CompletedTask;

            var game = rawGame as SkinGuessGame;

            if (game == null)
            {
                throw new DataException("Game 类型不匹配.");
            }

            return new
            {
                Game = FormatGame(game)
            };
        }

        public Task<object> GetGameStartPayload(Game game)
        {
            return Task.FromResult<object>(new {});
        }

        public Task<object> HandleMove(Game rawGame, string playerId, string move)
        {
            var game = rawGame as SkinGuessGame;
            var moveObj = JObject.Parse(move);

            if (game ==null || moveObj == null || moveObj["CharacterName"] == null)
            {
                throw new DataException("Move Payload 不合法.");
            }
            var characterName = moveObj["CharacterName"]!.ToString();

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
                    Completed = game.IsCompleted
                });
            }

            var answer = game.QuestionList[game.CurrentQuestionIndex];
            if (answer.CharacterName != characterName)
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
                    Result = "Wrong",
                    PlayerId = playerId,
                    CharacterName = characterName,
                    Completed = game.IsCompleted
                });
            }
            
            if (answer.PlayerId != null)
            {
                game.PlayerMoveList.Add(new PlayerMove()
                {
                    PlayerId = playerId,
                    CharacterName = characterName,
                    IsCorrect = false,
                    IsValid = true,
                });

                return Task.FromResult<object>(new { Result = "Answered", PlayerId = playerId, CharacterName = characterName, Answer = answer, Completed = game.IsCompleted });
            }

            answer.Completed = true;
            answer.AnswerTime = DateTime.Now;
            answer.PlayerId = playerId;
            

            if (game.PlayerScore.ContainsKey(playerId))
            {
                game.PlayerScore[playerId] += 200;
            }
            else
            {
                game.PlayerScore.TryAdd(playerId, 200);
            }

            game.PlayerMoveList.Add(new PlayerMove()
            {
                PlayerId = playerId,
                CharacterName = characterName,
                IsCorrect = true,
                IsValid = true,
            });

            game.CurrentQuestionIndex++;

            if (game.CurrentQuestionIndex >= game.QuestionList.Count)
            {
                game.IsCompleted = true;
                game.CompleteTime = DateTime.Now;

                manager.CreateStatistics(game);
            }


            return Task.FromResult<object>(new { 
                Result = "Correct", PlayerId = playerId,
                CharacterName = characterName, Answer = answer, Completed = game.IsCompleted,
                CurrentQuestionIndex = game.CurrentQuestionIndex,
                Game = FormatGame(game)
            });

        }

        public Task<object> GetCompleteGamePayload(Game rawGame)
        {
            var game = rawGame as SkinGuessGame;
            if (game == null)
            {
                throw new DataException("Game 类型不匹配.");
            }

            if (!game.IsCompleted)
            {
                game.IsCompleted = true;
                game.CompleteTime = DateTime.Now;

                manager.CreateStatistics(game);
            }


            return Task.FromResult<object>(new
            {
                GameId = game.Id,
                RemainingAnswers = game.QuestionList.Where(a => a.Completed == false)
            });
        }

        public Task<object> GetCloseGamePayload(Game rawGame)
        {
            var game = rawGame as SkinGuessGame;
            if (game == null)
            {
                throw new DataException("Game 类型不匹配.");
            }

            if (!game.IsCompleted)
            {
                game.IsCompleted = true;
                game.CompleteTime = DateTime.Now;

                manager.CreateStatistics(game);
            }


            return Task.FromResult<object>(new
            {
                GameId = game.Id,
                RemainingAnswers = game.QuestionList.Where(a => a.Completed == false)
            });
        }
        
        public Task<double> GetScore(Game game, string player)
        {
            var schulteGridGame = game as SkinGuessGame;

            if (schulteGridGame!.PlayerScore.ContainsKey(player))
            {
                return Task.FromResult(schulteGridGame.PlayerScore[player]);
            }

            return Task.FromResult<double>(0);
        }

        public Task<RequestHintOrGiveUpResult> GiveUp(Game rawGame, string appUserId)
        {
            var game = rawGame as SkinGuessGame;

            if (game == null)
            {
                throw new DataException("Game 类型不匹配.");
            }

            if (game.CreatorId != appUserId)
            {
                return Task.FromResult(new RequestHintOrGiveUpResult()
                {
                    Payload = new { Message = "只有房主可以放弃题目." },
                    GiveUpTriggered = false
                });
            }

            var answer = game.QuestionList[game.CurrentQuestionIndex];

            answer.Completed = true;
            answer.AnswerTime = DateTime.Now;
            game.CurrentQuestionIndex++;
            if (game.CurrentQuestionIndex >= game.QuestionList.Count)
            {
                game.IsCompleted = true;
                game.CompleteTime = DateTime.Now;
                manager.CreateStatistics(game);
            }

            return Task.FromResult(new RequestHintOrGiveUpResult()
            {
                Payload = new { 
                    Question = answer,
                    Game = FormatGame(game)
                },
                GiveUpTriggered = true
            });

        }

        public Task<RequestHintOrGiveUpResult> RequestHint(Game rawGame, string appUserId)
        {
            //判断是否进行过提示
            var game = rawGame as SkinGuessGame;

            if (game == null)
            {
                throw new DataException("Game 类型不匹配.");
            }

            var answer = game.QuestionList[game.CurrentQuestionIndex];

            if (answer.HintLevel == 0)
            {
                //第一次提示
                answer.HintLevel++;

                return Task.FromResult(new RequestHintOrGiveUpResult()
                {
                    Payload =
                        new
                        {
                            Question = answer,
                            CurrentQuestionIndex = game.CurrentQuestionIndex,
                            HintLevel = answer.HintLevel,
                            Game = FormatGame(game)
                        },
                    GiveUpTriggered = false,
                    HintTriggered = true
                });

            }
            else
            {
                //不允许再次提示
                return Task.FromResult(new RequestHintOrGiveUpResult()
                {
                    Payload = null,
                    GiveUpTriggered = false,
                    HintTriggered = false,
                });
            }

            
        }
    }
}
