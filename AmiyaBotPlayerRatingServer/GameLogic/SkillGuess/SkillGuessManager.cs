using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.Utility;
using Newtonsoft.Json.Linq;
using AmiyaBotPlayerRatingServer.Model;
using System.Data;

namespace AmiyaBotPlayerRatingServer.GameLogic.SkillGuess
{
    public class SkillGuessManager(ArknightsMemoryCache arknightsMemoryCache, PlayerRatingDatabaseContext dbContext)
        : IGameManager
    {
        private SkillGuessGame? GenerateRealGame()
        {
            var game = new SkillGuessGame();
            
            game.GameType = "SkillGuess";
            game.AnswerList = new List<SkillGuessGame.Answer>();

            var charMaps = arknightsMemoryCache.GetJson("character_table_full.json");
            var charSkillMap = charMaps?.JMESPathQuery("map(&{\"charId\":@.charId, \"name\":@.name, \"skills\":map(&{\"skillId\":@.skillId,\"skillName\":@.skillData.levels[0].name},to_array(@.skills))},values(@))");
            
            if (charMaps==null||charSkillMap == null)
            {
                //ERROR
                return null;
            }

            //获取一个全部技能列表，用来排除重复技能名称
            var allSkills = charSkillMap.SelectMany(x => x["skills"]!.Select(y => y["skillName"]!.ToString())).ToList();

            var operators = charMaps.ToList();
            var max = operators.Count;
            var random = new Random();

            while (game.AnswerList.Count < 16)
            {
                var rand = random.Next(0, max);
                var operatorData = operators[rand].Children().First();
                var charId = operatorData.Path;

                if (game.AnswerList.Any(x => x.CharacterId == charId))
                {
                    continue;
                }

                var charName = operatorData["name"]?.ToString();
                var skillList = operatorData["skills"]?.ToList();

                if (skillList == null || skillList.Count <= 1)
                {
                    continue;
                }

                //randomly select a skin except the first one
                var randSkill = random.Next(1, skillList.Count);
                var selectedSkill = skillList[randSkill];

                if (allSkills.Count(s => s == selectedSkill["skillName"]?.ToString())>1)
                {
                    continue;
                }

                var skillUrl = "https://web.hycdn.cn/arknights/game/assets/char_skill/" +
                               selectedSkill["skillId"] + ".png";

                var answer = new SkillGuessGame.Answer()
                {
                    CharacterName = charName!,
                    CharacterId = charId,
                    SkillName = selectedSkill["skillData"]?["levels"]?[0]?["name"]?.ToString()!,
                    SkillId = selectedSkill["skillId"]?.ToString()!,
                    ImageUrl = skillUrl,
                };
                game.AnswerList.Add(answer);
            }

            return game;
        }

        private bool IsOperatorName(string name)
        {
            var charDataJson = arknightsMemoryCache.GetJson("character_names.json")?.ToObject<Dictionary<String, String>>();
            if (charDataJson == null)
            {
                //ERROR
                return false;
            }

            return charDataJson.Values.Contains(name);
        }
        
        public Task<Game?> CreateNewGame(Dictionary<String, object> param)
        {
            var game = GenerateRealGame();
            return Task.FromResult<Game?>(game);
        }

        public Task<object> GetGameStartPayload(Game game)
        {
            return Task.FromResult<object>(new { });
        }


        private void CreateStatistics(SkillGuessGame game)
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
            var game = rawGame as SkillGuessGame;
            var moveObj = JObject.Parse(move);

            if (game == null || moveObj == null || moveObj["CharacterName"] == null)
            {
                throw new DataException("Move Payload不合法");
            }

            var characterName = moveObj["CharacterName"]!.ToString();

            if (!IsOperatorName(characterName))
            {
                game.PlayerMoveList.Add(new SkillGuessGame.PlayerMove()
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
                game.PlayerMoveList.Add(new SkillGuessGame.PlayerMove()
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
                game.PlayerMoveList.Add(new SkillGuessGame.PlayerMove()
                {
                    PlayerId = playerId,
                    CharacterName = characterName,
                    IsCorrect = false,
                    IsOperator = true,
                });
                return Task.FromResult<object>(new
                {
                    Result = "Answered",
                    PlayerId = playerId,
                    CharacterName = characterName,
                    Answer = answer,
                    Completed = game.IsCompleted
                });
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

            game.PlayerMoveList.Add(new SkillGuessGame.PlayerMove()
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

            return Task.FromResult<object>(new
            {
                Result = "Correct",
                PlayerId = playerId,
                CharacterName = characterName,
                Answer = answer,
                Completed = game.IsCompleted,
                CurrentQuestionIndex = game.CurrentQuestionIndex
            });

        }


        public Task<object> GetCompleteGamePayload(Game rawGame)
        {
            var game = rawGame as SkillGuessGame;

            if (game == null)
            {
                return Task.FromResult(new object());
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
            var game = rawGame as SkillGuessGame;

            if (game == null)
            {
                return Task.FromResult(new object());
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
            var game = rawGame as SkillGuessGame;
            if (game == null)
            {
                return Task.FromResult(new object());
            }
            if (game.IsStarted)
            {
                if (DateTime.Now - game.StartTime > TimeSpan.FromMinutes(60 * 3))
                {
                    if (game.IsCompleted == false)
                    {
                        game.IsCompleted = true;
                        game.CompleteTime = DateTime.Now;
                    }
                }
            }

            return Task.FromResult<object>(new
            {
                AnswerList = game.AnswerList,
                game.CurrentQuestionIndex,
            });
        }
        
        public Task<double> GetScore(Game rawGame, string player)
        {
            var game = rawGame as SkillGuessGame;

            if (game!.PlayerScore.ContainsKey(player))
            {
                return Task.FromResult(game.PlayerScore[player]);
            }

            return Task.FromResult<double>(0);
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
