using System.Data;
using System.Text.RegularExpressions;
using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.Model;
using AmiyaBotPlayerRatingServer.Utility;
using Newtonsoft.Json.Linq;

namespace AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid
{
    public class SchulteGridGameManager(ArknightsMemoryCache memoryCache, PlayerRatingDatabaseContext dbContext)
        : IGameManager
    {
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

        public async Task<Game?> CreateNewGame(Dictionary<String, JToken> param)
        {
            var game = await SchulteGridGameData.BuildContinuousMode(memoryCache);

            if(game == null)
            {
                return null;
            }

            var charMaps = memoryCache.GetJson("character_table_full.json");
            var charSkillMap = charMaps?.JMESPathQuery("map(&{\"charId\":@.charId, \"name\":@.name, \"skills\":map(&{\"skillId\":@.skillId,\"skillName\":@.skillData.levels[0].name},to_array(@.skills))},values(@))");

            foreach (var gridAnswer in game.AnswerList)
            {
                var charaName = gridAnswer.CharacterName;
                var charaId = charSkillMap?.FirstOrDefault(x => x["name"]?.ToString() == charaName)?["charId"]
                    ?.ToString();
                var skillName = gridAnswer.SkillName;
                var skillId = charSkillMap?.FirstOrDefault(x => x["name"]?.ToString() == charaName)?["skills"]?
                    .FirstOrDefault(x => Regex.Replace(x["skillName"]?.ToString()??"", @"[^\w]", "") == skillName)?["skillId"]?.ToString();
                gridAnswer.CharacterId = charaId!;
                gridAnswer.SkillId = skillId!;
            }

            return game;
        }

        public Task<object> GetGameStartPayload(Game game)
        {
            return Task.FromResult<object>(new { });
        }

        public Task<object> HandleMove(Game rawGame, string playerId, string move)
        {
            var game = rawGame as SchulteGridGame;
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
                game.PlayerMoveList.Add(new SchulteGridGame.PlayerMove()
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
                    Completed = game.IsCompleted,
                    CompleteTime = game.CompleteTime
                });
            }

            var answers = game.AnswerList.Where(a => a.CharacterName == characterName).ToList();
            if (answers.Count == 0)
            {
                game.PlayerMoveList.Add(new SchulteGridGame.PlayerMove()
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
                    Completed = game.IsCompleted,
                    CompleteTime = game.CompleteTime
                });
            }

            foreach (var answer in answers)
            {
                if (answer.PlayerId != null)
                {
                    game.PlayerMoveList.Add(new SchulteGridGame.PlayerMove()
                    {
                        PlayerId = playerId,
                        CharacterName = characterName,
                        IsCorrect = false,
                        IsOperator = true,
                    });

                    return Task.FromResult<object>(new
                    {
                        Result = "Answered", PlayerId = playerId, CharacterName = characterName, Answer = answer,
                        Completed = game.IsCompleted,
                        CompleteTime = game.CompleteTime
                    });
                }

                answer.Completed = true;
                answer.AnswerTime = DateTime.Now;
                answer.PlayerId = playerId;
            }
            
            game.PlayerMoveList.Add(new SchulteGridGame.PlayerMove()
            {
                PlayerId = playerId,
                CharacterName = characterName,
                IsCorrect = true,
                IsOperator = true,
            });

            if (game.PlayerScore.ContainsKey(playerId))
            {
                game.PlayerScore[playerId] += 200;
            }
            else
            {
                game.PlayerScore.TryAdd(playerId, 200);
            }

            if (game.IsCompleted != true)
            {
                if (game.AnswerList.All(a => a.Completed))
                {
                    game.IsCompleted = true;
                    game.CompleteTime = DateTime.Now;

                    CreateStatistics(game);
                }
            }

            return Task.FromResult<object>(new
            {
                Result = "Correct", PlayerId = playerId, CharacterName = characterName, Answer = answers,
                Completed = game.IsCompleted,
                CompleteTime = game.CompleteTime
            });

        }

        public Task<object> GetCompleteGamePayload(Game rawGame)
        {
            var game = rawGame as SchulteGridGame;

            if (game == null)
            {
                return Task.FromResult<object>(new { });
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
                RemainingAnswers = game.AnswerList.Where(a => a.Completed == false),
                IsCompleted = game.IsCompleted,
                CompleteTime = game.CompleteTime,
                IsClosed = true,
                CloseTime = game.CloseTime
            });
        }

        public Task<object> GetCloseGamePayload(Game rawGame)
        {
            var game = rawGame as SchulteGridGame;

            if (game == null)
            {
                return Task.FromResult<object>(new { });
            }

            if (!game.IsCompleted)
            {
                game.IsCompleted = true;
                game.CompleteTime = DateTime.Now;
                CreateStatistics(game);
            }
            
            return Task.FromResult<object>(new { GameId= game.Id, 
                RemainingAnswers = game.AnswerList.Where(a=>a.Completed==false),
                IsCompleted = game.IsCompleted,
                CompleteTime = game.CompleteTime,
                IsClosed = true,
                CloseTime = game.CloseTime
            });
        }

        private void CreateStatistics(SchulteGridGame game)
        {
            //题目没有回答超过一半的游戏不计入统计
            if (game.AnswerList.Count(a => a.Completed) < game.AnswerList.Count / 2)
            {
                return;
            }

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

        public async Task<object> GetGamePayload(Game rawGame)
        {
            await Task.CompletedTask;

            var game = rawGame as SchulteGridGame;

            if (game == null)
            {
                return Task.FromResult<object>(new { });
            }
            
            return new
            {
                AnswerList = game.AnswerList.Where(a=>a.Completed),
                RemainingAnswers = game.IsCompleted ? game.AnswerList.Where(a => !a.Completed) : [],
                Grid = game.Grid,
            };
        }

        public async Task<double> GetScore(Game game, string player)
        {
            await Task.CompletedTask;

            var schulteGridGame = game as SchulteGridGame;
            
            if (schulteGridGame!.PlayerScore.ContainsKey(player))
            {
                return schulteGridGame.PlayerScore[player];
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
