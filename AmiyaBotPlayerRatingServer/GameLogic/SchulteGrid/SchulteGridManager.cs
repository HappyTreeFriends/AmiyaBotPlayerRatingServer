using System.Text.RegularExpressions;
using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.Model;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid
{
    public class SchulteGridGameManager : IGameManager
    {
        private readonly ArknightsMemoryCache _arknightsMemoryCache;
        private readonly PlayerRatingDatabaseContext _dbContext;

        public SchulteGridGameManager(ArknightsMemoryCache memoryCache,PlayerRatingDatabaseContext dbContext)
        {
            _arknightsMemoryCache = memoryCache;
            _dbContext = dbContext;
        }
        
        public async Task<Game?> CreateNewGame(Dictionary<String, JToken> param)
        {
            var game = await SchulteGridGameData.BuildContinuousMode(_arknightsMemoryCache);
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
            var characterName = moveObj["CharacterName"].ToString();

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

            if (!SchulteGridGameData.IsOperator(characterName))
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
                if (game.AnswerList.All(a => a.Completed == true))
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

        public Task<object> GetCloseGamePayload(Game rawGame)
        {
            var game = rawGame as SchulteGridGame;

            if (!game.IsCompleted)
            {
                game.IsCompleted = true;
                game.CompleteTime = DateTime.Now;
            }

            CreateStatistics(game);

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
                //统计第一名第二名和第三名
                var playerScoreList = game.PlayerScore.ToList();
                playerScoreList.Sort((a, b) => b.Value.CompareTo(a.Value));

                var firstPlace = playerScoreList[0];
                var secondPlace = playerScoreList.Count > 1 ? playerScoreList[1] : default;
                var thirdPlace = playerScoreList.Count > 2 ? playerScoreList[2] : default;

                //统计每个玩家的正确和错误次数
                var playerAnswerList = game.PlayerMoveList.Where(x => x.IsOperator == true).GroupBy(p => p.PlayerId)
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
                        _dbContext.ApplicationUserMinigameStatistics.FirstOrDefault(x => x.UserId == playerId);
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
                        _dbContext.ApplicationUserMinigameStatistics.Add(playerSt);
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

                    _dbContext.SaveChanges();
                }
        }

        public async Task<object> GetGamePayload(Game game)
        {
            await Task.CompletedTask;

            var schulteGridGame = game as SchulteGridGame;

            if (schulteGridGame.IsStarted)
            {
                if (DateTime.Now - schulteGridGame.StartTime > TimeSpan.FromMinutes(60*3))
                {
                    if (schulteGridGame.IsCompleted == false)
                    {
                        schulteGridGame.IsCompleted = true;
                        schulteGridGame.CompleteTime = DateTime.Now;
                    }
                }
            }

            return new
            {
                AnswerList = schulteGridGame!.AnswerList.Where(a=>a.Completed==true),
                Grid = schulteGridGame.Grid,
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
