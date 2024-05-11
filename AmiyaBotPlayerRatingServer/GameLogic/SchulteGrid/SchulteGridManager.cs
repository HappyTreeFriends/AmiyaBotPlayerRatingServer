using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid
{
    public class SchulteGridGameManager: GameManager
    {
        private static JObject? _characterMap;

        private static void LoadCharacterMap()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "CharacterMap.json");

            if (System.IO.File.Exists(filePath))
            {
                var fileStream = new FileStream(filePath, FileMode.Open);
                using var reader = new StreamReader(fileStream);
                _characterMap = JObject.Parse(reader.ReadToEnd());
            }
        }

        public static void Init()
        {
            LoadCharacterMap();
        }


        public override String CreateNewGame()
        {
            var game = new SchulteGridGame();
            
            //手动添加一些测试数据
            game.GridWidth = 4;
            game.GridHeight = 4;
            game.Grid = new string[,]
            {
                {"情", "坚", "守", "模"},
                {"共", "崩", "毁", "式"},
                {"恸", "见", "之", "判"},
                {"哀", "我", "前", "决"}
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
                return JsonConvert.SerializeObject(new { Result = "Wrong" });
            }

            return JsonConvert.SerializeObject(new { Result = "Correct", Answer = answer , Completed = false});

        }
    }
}
