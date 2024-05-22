using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.GameLogic.SkinGuess;
using AmiyaBotPlayerRatingServer.Utility;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using static System.Net.WebRequestMethods;

namespace AmiyaBotPlayerRatingServer.GameLogic.SkillGuess
{
    public class SkillGuessManager : GameManager
    {
        private readonly ArknightsMemoryCache _arknightsMemoryCache;

        public SkillGuessManager(ArknightsMemoryCache arknightsMemoryCache)
        {
            _arknightsMemoryCache = arknightsMemoryCache;
        }

        private static int RandomNumberGenerator() => new Random().Next(0, 100000);

        
        private SkillGuessGame GenerateRealGame()
        {
            var game = new SkillGuessGame();
            
            game.GameType = "SkillGuess";
            game.AnswerList = new List<SkillGuessGame.Answer>();

            var charMaps = _arknightsMemoryCache.GetJson("character_table_full.json");
            var charSkillMap = charMaps?.JMESPathQuery("map(&{\"charId\":@.charId, \"name\":@.name, \"skills\":map(&{\"skillId\":@.skillId,\"skillName\":@.skillData.levels[0].name},to_array(@.skills))},values(@))");
            
            if (charSkillMap == null)
            {
                //ERROR
                return null;
            }

            //获取一个全部技能列表，用来排除重复技能名称
            var allSkills = charSkillMap.SelectMany(x => x["skills"].Select(y => y["skillName"].ToString())).ToList();

            var operators = charMaps.ToList();
            var max = operators.Count();
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
                var skillList = operatorData["skills"].ToList();

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
                               selectedSkill["skillId"]?.ToString() + ".png";

                var answer = new SkillGuessGame.Answer()
                {
                    CharacterName = charName,
                    CharacterId = charId,
                    SkillName = selectedSkill["skillData"]["levels"][0]["name"]?.ToString(),
                    SkillId = selectedSkill["skillId"]?.ToString(),
                    ImageUrl = skillUrl,
                };
                game.AnswerList.Add(answer);
            }

            return game;
        }

        private bool IsOperatorName(string name)
        {
            var charDataJson = _arknightsMemoryCache.GetJson("character_names.json")?.ToObject<Dictionary<String, String>>();
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

        public override Task GetGameStartPayload(Game game)
        {
            return Task.CompletedTask;
        }

        public override string HandleMove(Game rawGame, string playerId, string move)
        {
            var game = rawGame as SkillGuessGame;

            var moveObj = JObject.Parse(move);
            var characterName = moveObj["CharacterName"].ToString();

            if (!IsOperatorName(characterName))
            {
                return JsonConvert.SerializeObject(new
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
                return JsonConvert.SerializeObject(new
                {
                    Result = "Wrong",
                    PlayerId = playerId,
                    CharacterName = characterName,
                    Completed = game.IsCompleted
                });
            }

            if (answer.PlayerId != null)
            {
                return JsonConvert.SerializeObject(new { Result = "Answered", PlayerId = playerId, CharacterName = characterName, Answer = answer, Completed = game.IsCompleted });
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

            return JsonConvert.SerializeObject(new
            {
                Result = "Correct",
                PlayerId = playerId,
                CharacterName = characterName,
                Answer = answer,
                Completed = game.IsCompleted,
                CurrentQuestionIndex = game.CurrentQuestionIndex
            });

        }

        public override string GetCloseGamePayload(Game rawGame)
        {
            var game = rawGame as SkillGuessGame;
            return JsonConvert.SerializeObject(new { GameId = game.Id, RemainingAnswers = game.AnswerList.Where(a => a.Completed == false) });
        }

        public override object GetGamePayload(Game game)
        {
            var game = rawGame as SkillGuessGame;

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

            return new
            {
                AnswerList = game!.AnswerList,
                game.CurrentQuestionIndex,
            };
        }

        public override double GetScore(Game rawGame, string player)
        {
            var game = rawGame as SkillGuessGame;

            if (game!.PlayerScore.ContainsKey(player))
            {
                return game.PlayerScore[player];
            }

            return 0;
        }
    }
}
