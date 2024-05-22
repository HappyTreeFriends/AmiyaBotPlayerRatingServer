using System.Text.RegularExpressions;
using AmiyaBotPlayerRatingServer.Data;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid
{
    public class SchulteGridGameManager : GameManager
    {
        private readonly ArknightsMemoryCache _arknightsMemoryCache;

        public SchulteGridGameManager(ArknightsMemoryCache memoryCache)
        {
            _arknightsMemoryCache = memoryCache;
        }

        private static SchulteGridGame GenerateTestGame()
        {
            var game = new SchulteGridGame();

            //手动添加一些测试数据
            game.GameType = "SchulteGrid";
            game.GridWidth = 4;
            game.GridHeight = 4;
            game.Grid = new List<List<string>>()
            {
                new List<string>{"情", "坚", "守", "模"},
                new List<string>{"共", "崩", "毁", "式"},
                new List<string>{"恸", "见", "之", "判"},
                new List<string>{"哀", "我", "前", "决"}
            };
            game.AnswerList = new List<SchulteGridGame.GridAnswer>()
            {
                new SchulteGridGame.GridAnswer()
                {
                    CharacterName = "凛视",
                    SkillName = "我见崩毁之前",
                    GridPointList = new List<SchulteGridGame.GridPoint>()
                    {
                        new SchulteGridGame.GridPoint() {X = 1, Y = 3},
                        new SchulteGridGame.GridPoint() {X = 1, Y = 2},
                        new SchulteGridGame.GridPoint() {X = 1, Y = 1},
                        new SchulteGridGame.GridPoint() {X = 2, Y = 1},
                        new SchulteGridGame.GridPoint() {X = 2, Y = 2},
                        new SchulteGridGame.GridPoint() {X = 2, Y = 3},
                    }
                },
                new SchulteGridGame.GridAnswer()
                {
                    CharacterName = "艾丽妮",
                    SkillName = "判决",
                    GridPointList = new List<SchulteGridGame.GridPoint>()
                    {
                        new SchulteGridGame.GridPoint() {X = 3, Y = 2},
                        new SchulteGridGame.GridPoint() {X = 3, Y = 3},
                    }
                },
                new SchulteGridGame.GridAnswer()
                {
                    CharacterName = "火神",
                    SkillName = "坚守模式",
                    GridPointList = new List<SchulteGridGame.GridPoint>()
                    {
                        new SchulteGridGame.GridPoint() {X = 1, Y = 0},
                        new SchulteGridGame.GridPoint() {X = 2, Y = 0},
                        new SchulteGridGame.GridPoint() {X = 3, Y = 0},
                        new SchulteGridGame.GridPoint() {X = 3, Y = 1},
                    }
                },
                new SchulteGridGame.GridAnswer()
                {
                    CharacterName = "阿米娅",
                    SkillName = "哀恸共情",
                    GridPointList = new List<SchulteGridGame.GridPoint>()
                    {
                        new SchulteGridGame.GridPoint() {X = 0, Y = 3},
                        new SchulteGridGame.GridPoint() {X = 0, Y = 2},
                        new SchulteGridGame.GridPoint() {X = 0, Y = 1},
                        new SchulteGridGame.GridPoint() {X = 0, Y = 0},
                    }
                },
            };

            return game;
        }

        public override async Task<Game> CreateNewGame(Dictionary<String, JToken> param)
        {
            var game = await SchulteGridGameData.BuildContinuousMode(_arknightsMemoryCache);
            return game;
        }

        public override Task<object> GetGameStartPayload(Game game)
        {
            return Task.FromResult<object>(new { });
        }

        public override Task<object> HandleMove(Game rawGame, string playerId, string move)
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
                }
            }

            return Task.FromResult<object>(new
            {
                Result = "Correct", PlayerId = playerId, CharacterName = characterName, Answer = answers,
                Completed = game.IsCompleted,
                CompleteTime = game.CompleteTime
            });

        }

        public override Task<object> GetCloseGamePayload(Game rawGame)
        {
            var game = rawGame as SchulteGridGame;

            if (!game.IsCompleted)
            {
                game.IsCompleted = true;
                game.CompleteTime = DateTime.Now;
            }

            return Task.FromResult<object>(new { GameId= game.Id, 
                RemainingAnswers = game.AnswerList.Where(a=>a.Completed==false),
                IsCompleted = game.IsCompleted,
                CompleteTime = game.CompleteTime,
                IsClosed = true,
                CloseTime = game.CloseTime
            });
        }

        public override async Task<object> GetGamePayload(Game game)
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

        public override async Task<double> GetScore(Game game, string player)
        {
            await Task.CompletedTask;

            var schulteGridGame = game as SchulteGridGame;
            
            if (schulteGridGame!.PlayerScore.ContainsKey(player))
            {
                return schulteGridGame.PlayerScore[player];
            }

            return 0;
        }
    }
}
