using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AmiyaBotPlayerRatingServer.GameLogic.SkinGuess
{
    public class SkinGuessManager : GameManager
    {
        private readonly ArknightsMemoryCache _arknightsMemoryCache;

        public SkinGuessManager(ArknightsMemoryCache arknightsMemoryCache)
        {
            _arknightsMemoryCache = arknightsMemoryCache;
        }

        private static int RandomNumberGenerator ()=> new Random().Next(0, 100000);

        private static SkinGuessGame GenerateTestGame()
        {
            var game = new SkinGuessGame();

            //手动添加一些测试数据
            game.GameType = "SkinGuess";
            game.AnswerList = new List<SkinGuessGame.Answer>()
            {
                new SkinGuessGame.Answer()
                {
                    CharacterName = "雷蛇",
                    CharacterId = "char_107_liskam",
                    SkinName = "春竜",
                    SkinId = "char_107_liskam@nian#2",
                    ImageUrl = "https://media.prts.wiki/3/3d/%E7%AB%8B%E7%BB%98_%E9%9B%B7%E8%9B%87_skin1.png",
                    RandomNumber = RandomNumberGenerator()
                },
                new SkinGuessGame.Answer()
                {
                    CharacterName = "霜叶",
                    CharacterId = "char_109_silent",
                    SkinName = "破晓",
                    SkinId = "char_109_silent@nian#2",
                    ImageUrl = "https://media.prts.wiki/6/6e/%E7%AB%8B%E7%BB%98_%E9%9C%9C%E5%8F%B6_skin1.png",
                    RandomNumber = RandomNumberGenerator()
                },


            };

            return game;
        }


        public override async Task<Game> CreateNewGame(string param)
        {
            var paramObj = JObject.Parse(param);

            var game = GenerateTestGame();
            game.IsPrivate = paramObj["IsPrivate"]?.ToObject<bool>() ?? false;
            return game;
        }

        public override Task GameStart(Game game)
        {
            return Task.CompletedTask;
        }

        public override string HandleMove(Game rawGame, string playerId, string move)
        {
            var game = rawGame as SkinGuessGame;

            var moveObj = JObject.Parse(move);
            var characterName = moveObj["CharacterName"].ToString();

            if (!SchulteGridGameData.IsOperator(characterName))
            {
                return JsonConvert.SerializeObject(new
                {
                    Result = "NotOperator",
                    PlayerId = playerId,
                    CharacterName = characterName,
                    Completed = game.IsCompleted
                });
            }

            var answers = game.AnswerList.Where(a => a.CharacterName == characterName).ToList();
            if (answers.Count == 0)
            {
                return JsonConvert.SerializeObject(new
                {
                    Result = "Wrong",
                    PlayerId = playerId,
                    CharacterName = characterName,
                    Completed = game.IsCompleted
                });
            }

            foreach (var answer in answers)
            {
                if (answer.PlayerId != null)
                {
                    return JsonConvert.SerializeObject(new { Result = "Answered", PlayerId = playerId, CharacterName = characterName, Answer = answer, Completed = game.IsCompleted });
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

            return JsonConvert.SerializeObject(new { Result = "Correct", PlayerId = playerId, CharacterName = characterName, Answer = answers, Completed = game.IsCompleted });

        }

        public override string CloseGame(Game rawGame)
        {
            var game = rawGame as SkinGuessGame;
            return JsonConvert.SerializeObject(new { GameId = game.Id, RemainingAnswers = game.AnswerList.Where(a => a.Completed == false) });
        }

        public override object GetGameStatus(Game game)
        {
            var schulteGridGame = game as SchulteGridGame;

            if (schulteGridGame.IsStarted)
            {
                if (DateTime.Now - schulteGridGame.StartTime > TimeSpan.FromMinutes(60 * 3))
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
                AnswerList = schulteGridGame!.AnswerList.Where(a => a.Completed == true),
                Grid = schulteGridGame.Grid,
            };
        }

        public override double GetScore(Game game, string player)
        {
            var schulteGridGame = game as SkinGuessGame;

            if (schulteGridGame!.PlayerScore.ContainsKey(player))
            {
                return schulteGridGame.PlayerScore[player];
            }

            return 0;
        }
    }
}
