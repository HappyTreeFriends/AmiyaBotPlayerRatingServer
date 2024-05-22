using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;
using AmiyaBotPlayerRatingServer.Utility;
using Hangfire.Common;
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
                    ImageUrl = "https://media.prts.wiki/8/8c/立绘_霜叶_skin1.png",
                    RandomNumber = RandomNumberGenerator()
                },
            };

            return game;
        }

        private SkinGuessGame GenerateRealGame()
        {
            var game = new SkinGuessGame();

            //手动添加一些测试数据
            game.GameType = "SkinGuess";
            game.AnswerList= new List<SkinGuessGame.Answer>();

            var charMaps = _arknightsMemoryCache.GetJson("character_names.json")?.ToObject<Dictionary<String,String>>();
            var skinUrls = _arknightsMemoryCache.GetJson("skinUrls.json") as JObject;

            if (charMaps == null || skinUrls == null)
            {
                //ERROR
                return GenerateTestGame();
            }
            
            var operatorIdList = charMaps.Keys.ToList();
            var max = operatorIdList.Count();
            var random = new Random();

            while(game.AnswerList.Count<15)
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
                var skinUrl = skinList[randSkin]?.ToString();

                if (skinUrl == null)
                {
                    continue;
                }

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
            var charDataJson = _arknightsMemoryCache.GetJson("character_names.json")?.ToObject<Dictionary<String,String>>();
            if (charDataJson == null)
            {
                //ERROR
                return false;
            }

            return charDataJson.Values.Contains(name);
        }

        public override Task<Game> CreateNewGame(Dictionary<String, JToken> param)
        {
            var game = GenerateRealGame();
            return Task.FromResult<Game>(game);
        }

        public override Task<object> GetGameStartPayload(Game game)
        {
            return Task.FromResult<object>(new {});
        }

        public override Task<object> HandleMove(Game rawGame, string playerId, string move)
        {
            var game = rawGame as SkinGuessGame;

            var moveObj = JObject.Parse(move);
            var characterName = moveObj["CharacterName"].ToString();

            if (!IsOperatorName(characterName))
            {
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

            game.CurrentQuestionIndex++;

            if (game.CurrentQuestionIndex >= game.AnswerList.Count)
            {
                game.IsCompleted = true;
                game.CompleteTime = DateTime.Now;
            }

            return Task.FromResult<object>(new { Result = "Correct", PlayerId = playerId,
                CharacterName = characterName, Answer = answer, Completed = game.IsCompleted,
                CurrentQuestionIndex = game.CurrentQuestionIndex
            });

        }

        public override Task<object> GetCloseGamePayload(Game rawGame)
        {
            var game = rawGame as SkinGuessGame;
            return Task.FromResult<object>(new { GameId = game.Id, RemainingAnswers = game.AnswerList.Where(a => a.Completed == false) });
        }

        public override Task<object> GetGamePayload(Game game)
        {
            var schulteGridGame = game as SkinGuessGame;

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

            return Task.FromResult<object>(new
            {
                AnswerList = schulteGridGame!.AnswerList,
                schulteGridGame.CurrentQuestionIndex,
            });
        }

        public override Task<double> GetScore(Game game, string player)
        {
            var schulteGridGame = game as SkinGuessGame;

            if (schulteGridGame!.PlayerScore.ContainsKey(player))
            {
                return Task.FromResult<double>(schulteGridGame.PlayerScore[player]);
            }

            return Task.FromResult<double>(0);
        }
    }
}
