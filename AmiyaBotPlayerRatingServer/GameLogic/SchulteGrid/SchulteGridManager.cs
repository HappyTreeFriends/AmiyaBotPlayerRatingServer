using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid
{
    public class SchulteGridGameManager : GameManager
    {

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

        public override async Task<String> CreateNewGame()
        {
            var game = await SchulteGridGameData.BuildContinuousMode();

            GameManager.GameList.Add(game);

            string gameId;
            do
            {
                gameId = new Random().Next(100000, 999999).ToString();
            } while (GameManager.GameList.Any(g => g.GameId == gameId));

            game.GameId = gameId;

            return gameId;
        }

        public override string HandleMove(Game rawGame, string contextConnectionId, string move)
        {
            var game = rawGame as SchulteGridGame;

            var moveObj = JObject.Parse(move);
            var characterName = moveObj["CharacterName"].ToString();

            var answer = game.AnswerList.Find(a => a.CharacterName == characterName);
            if (answer == null)
            {
                return JsonConvert.SerializeObject(new { Result = "Wrong", CharacterName = characterName, Completed = false });
            }

            if (answer.PlayerId != null)
            {
                return JsonConvert.SerializeObject(new { Result = "Wrong", CharacterName = characterName, Answer = answer, Completed = false });
            }

            answer.Completed = true;
            answer.AnswerTime = DateTime.Now;
            answer.PlayerId = contextConnectionId;

            if (game.PlayerScore.ContainsKey(contextConnectionId))
            {
                game.PlayerScore[contextConnectionId] += 200;
            }
            else
            {
                game.PlayerScore.TryAdd(contextConnectionId, 200);
            }

            return JsonConvert.SerializeObject(new { Result = "Correct", CharacterName = characterName, Answer = answer, Completed = game.AnswerList.All(a=>a.Completed==true) });

        }

        public override double GetScore(Game game, string player)
        {
            var schulteGridGame = game as SchulteGridGame;
            
            if (schulteGridGame.PlayerScore.ContainsKey(player))
            {
                return schulteGridGame.PlayerScore[player];
            }

            return 0;
        }
    }
}
