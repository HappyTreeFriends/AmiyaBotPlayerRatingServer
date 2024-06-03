using System.Data;
using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.Model;
using Newtonsoft.Json.Linq;
using static AmiyaBotPlayerRatingServer.GameLogic.IGameManager;

namespace AmiyaBotPlayerRatingServer.GameLogic.SkinGuess
{
    public class SkinGuessManager(ArknightsMemoryCache arknightsMemoryCache, PlayerRatingDatabaseContext dbContext)
        : IGameManager
    {
        private static int RandomNumberGenerator ()=> new Random().Next(0, 100000);

        private SkinGuessGame GenerateTestGame()
        {
            var game = new SkinGuessGame();

            //手动添加一些测试数据
            game.GameType = "SkinGuess";
            game.AnswerList =
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
            game.AnswerList= new List<SkinGuessGame.Answer>();

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

            while(game.AnswerList.Count<16)
            {
                var rand = random.Next(0, max);
                var charId = operatorIdList[rand];
                if (game.AnswerList.Any(x => x.CharacterId == charId))
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
                
                var answer = new SkinGuessGame.Answer()
                {
                    CharacterName = charName,
                    CharacterId = charId,
                    SkinName = "",
                    SkinId = "",
                    ImageUrl = skinUrl,
                    RandomNumber = RandomNumberGenerator()
                };
                game.AnswerList.Add(answer);
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
        
        public Task<Game?> CreateNewGame(Dictionary<String, JToken> param)
        {
            var game = GenerateRealGame();
            return Task.FromResult<Game?>(game);
        }

        public Task<object> GetGameStartPayload(Game game)
        {
            return Task.FromResult<object>(new {});
        }

        private void CreateStatistics(SkinGuessGame game)
        {
            //统计第一名第二名和第三名
            var playerScoreList = game.PlayerScore.ToList();
            playerScoreList.Sort((a, b) => b.Value.CompareTo(a.Value));

            var firstPlace = playerScoreList.Count > 0 ? playerScoreList[0] : default;
            var secondPlace = playerScoreList.Count > 1 ? playerScoreList[1] : default;
            var thirdPlace = playerScoreList.Count > 2 ? playerScoreList[2] : default;

            //统计每个玩家的正确和错误次数
            var playerAnswerList = game.PlayerMoveList.Where(x => x.IsOperator).GroupBy(p => p.PlayerId)
                .Select(p => new
                {
                    PlayerId = p.Key,
                    CorrectCount = p.Count(c => c.IsCorrect),
                    WrongCount = p.Count(c => !c.IsCorrect)
                }).ToList();

            foreach (var pl in game.PlayerList)
            {
                var playerId = pl.Key;
                var playerSt =
                    dbContext.ApplicationUserMinigameStatistics.FirstOrDefault(x => x.UserId == playerId);
                if (playerSt == null)
                {
                    playerSt = new ApplicationUserMinigameStatistics()
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = playerId,
                        TotalGamesPlayed = 0,
                        TotalGamesFirstPlace = 0,
                        TotalGamesSecondPlace = 0,
                        TotalGamesThirdPlace = 0,
                        TotalAnswersCorrect = 0,
                        TotalAnswersWrong = 0
                    };
                    dbContext.ApplicationUserMinigameStatistics.Add(playerSt);
                }

                playerSt.TotalGamesPlayed++;
                playerSt.TotalAnswersCorrect +=
                    playerAnswerList.FirstOrDefault(x => x.PlayerId == playerId)?.CorrectCount ?? 0;
                playerSt.TotalAnswersWrong +=
                    playerAnswerList.FirstOrDefault(x => x.PlayerId == playerId)?.WrongCount ?? 0;

                if (firstPlace.Key == playerId)
                {
                    playerSt.TotalGamesFirstPlace++;
                }
                else if (secondPlace.Key == playerId)
                {
                    playerSt.TotalGamesSecondPlace++;
                }
                else if (thirdPlace.Key == playerId)
                {
                    playerSt.TotalGamesThirdPlace++;
                }

                dbContext.SaveChanges();
            }
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
                game.PlayerMoveList.Add(new SkinGuessGame.PlayerMove()
                {
                    PlayerId = playerId,
                    CharacterName = characterName,
                    IsCorrect = false,
                    IsOperator = false,
                });

                return Task.FromResult<object>(new
                {
                    Result = "NotOperator",
                    PlayerId = playerId,
                    CharacterName = characterName,
                    Completed = game.IsCompleted
                });
            }

            var answer = game.AnswerList[game.CurrentQuestionIndex];
            if (answer.CharacterName != characterName)
            {
                game.PlayerMoveList.Add(new SkinGuessGame.PlayerMove()
                {
                    PlayerId = playerId,
                    CharacterName = characterName,
                    IsCorrect = false,
                    IsOperator = true,
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
                game.PlayerMoveList.Add(new SkinGuessGame.PlayerMove()
                {
                    PlayerId = playerId,
                    CharacterName = characterName,
                    IsCorrect = false,
                    IsOperator = true,
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

            game.PlayerMoveList.Add(new SkinGuessGame.PlayerMove()
            {
                PlayerId = playerId,
                CharacterName = characterName,
                IsCorrect = true,
                IsOperator = true,
            });

            game.CurrentQuestionIndex++;

            if (game.CurrentQuestionIndex >= game.AnswerList.Count)
            {
                game.IsCompleted = true;
                game.CompleteTime = DateTime.Now;

                CreateStatistics(game);
            }


            return Task.FromResult<object>(new { Result = "Correct", PlayerId = playerId,
                CharacterName = characterName, Answer = answer, Completed = game.IsCompleted,
                CurrentQuestionIndex = game.CurrentQuestionIndex
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

                CreateStatistics(game);
            }


            return Task.FromResult<object>(new
            {
                GameId = game.Id,
                RemainingAnswers = game.AnswerList.Where(a => a.Completed == false)
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

                CreateStatistics(game);
            }


            return Task.FromResult<object>(new
            {
                GameId = game.Id,
                RemainingAnswers = game.AnswerList.Where(a => a.Completed == false)
            });
        }

        public Task<object> GetGamePayload(Game rawGame)
        {
            var game = rawGame as SkinGuessGame;

            if (game == null)
            {
                throw new DataException("Game 类型不匹配.");
            }
            
            return Task.FromResult<object>(new
            {
                AnswerList = game.AnswerList,
                CurrentQuestionIndex = game.CurrentQuestionIndex,
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

            var answer = game.AnswerList[game.CurrentQuestionIndex];

            answer.Completed = true;
            answer.AnswerTime = DateTime.Now;
            game.CurrentQuestionIndex++;
            if (game.CurrentQuestionIndex >= game.AnswerList.Count)
            {
                game.IsCompleted = true;
                game.CompleteTime = DateTime.Now;
                CreateStatistics(game);
            }

            return Task.FromResult(new RequestHintOrGiveUpResult()
            {
                Payload = new { Question = answer },
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

            var answer = game.AnswerList[game.CurrentQuestionIndex];

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
                            HintLevel = answer.HintLevel
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
                    Payload = answer,
                    GiveUpTriggered = false,
                    HintTriggered = false,
                });
            }

            
        }
    }
}
