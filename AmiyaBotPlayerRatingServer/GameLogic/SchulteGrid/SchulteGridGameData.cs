using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using AmiyaBotPlayerRatingServer.Data;

namespace AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid
{
    public static class SchulteGridGameData
    {
        public static void LoadCharacterMap(ArknightsMemoryCache memCache)
        {

            var namesDict = new Dictionary<string, Dictionary<string, Object>>();

            if (memCache.GetJson("skill_table.json") is JObject skillMap)
            {
                var skillDict = BuildNameDict(memCache, (op) =>
                {
                    var skills = new List<String>();
                    if (op["skills"] is JArray skillsArray)
                    {
                        foreach (var skill in skillsArray)
                        {
                            var skillId = skill["skillId"]?.ToString();
                            if (skillId == null) continue;
                            if (skillMap[skillId] is JObject skillData)
                            {
                                var skillName = skillData["levels"]?[0]?["name"]?.ToString();
                                if (skillName != null)
                                {
                                    skills.Add(skillName);
                                }
                            }
                        }
                    }

                    return skills;
                });
                if(skillDict!=null)
                    namesDict.Add("skill", skillDict);
            }

            memCache.LoadObject(namesDict, "schulte_grid_names_dict.json");
        }
        
        private static Dictionary<string, object>? BuildNameDict(ArknightsMemoryCache memCache,
            Func<JObject, List<String>> dataFunction)
        {
            var characterMap = memCache.GetJson("character_table.json") as JObject;
            var operatorList = memCache.GetJson("character_names.json")!.ToObject<Dictionary<String, String>>();
            if (characterMap == null || operatorList == null)
            {
                return null;
            }

            Dictionary<string, string> tempDict = new Dictionary<string, string>();
            List<string> nameKeys = new List<string>();
            Dictionary<string, string> dataDict = new Dictionary<string, string>();
            List<string> blackList = new List<string>();
            
            foreach (var op in operatorList)
            {
                var opObject = characterMap[op.Key] as JObject;
                if (opObject == null)
                {
                    continue;
                }

                var items = dataFunction(opObject);
                foreach (var item in items)
                {
                    string itemName = Regex.Replace(item, @"[^\w]", "");
                    tempDict[itemName] = opObject["name"]?.ToString()!;
                    nameKeys.Add(itemName);
                }
            }

            foreach (var itemName in tempDict.Keys)
            {
                if (nameKeys.FindAll(x => x == itemName).Count > 1)
                {
                    blackList.Add(itemName);
                    continue;
                }
                dataDict[itemName] = tempDict[itemName];
            }

            return new Dictionary<string, object>
            {
                { "data", dataDict },
                { "blackList", blackList }
            };
        }

        public static async Task<SchulteGridGame?> BuildContinuousMode(ArknightsMemoryCache arknightsMemoryCache)
        {
            var namesDict = arknightsMemoryCache.GetJson("schulte_grid_names_dict.json")!.ToObject<Dictionary<string, JToken>>();

            if (namesDict == null)
            {
                return null;
            }

            var wordType = "skill";
            var namesDictObject = namesDict[wordType];
            var wordsMap = namesDictObject["data"]!.ToObject<Dictionary<string, string>>();
            var words = wordsMap.Keys.ToList();
            var blackList = namesDictObject["blackList"]!.ToObject<List<string>>();
            
            var (puzzle,answer) = await SchulteGridContinuousGameBuilder.BuildPuzzleContinuousMode(10, 10, words, blackList,3);

            if(puzzle==null||answer==null)
                return null;

            var game = new SchulteGridGame();
            
            game.GameType = "SchulteGrid";
            game.GridWidth = 10;
            game.GridHeight = 10;
            game.Grid = new List<List<string>>();
            for (int i = 0; i < 10; i++)
            {
                game.Grid.Add(new List<string>());
                for (int j = 0; j < 10; j++)
                {
                    game.Grid[i].Add(puzzle[i, j].ToString());
                }
            }

            game.AnswerList = new List<SchulteGridGame.GridAnswer>();
            foreach (var item in answer)
            {
                game.AnswerList.Add(new SchulteGridGame.GridAnswer()
                {
                    CharacterName = wordsMap[item.Key],
                    SkillName = item.Key,
                    GridPointList = new List<SchulteGridGame.GridPoint>()
                });

                foreach (var point in item.Value)
                {
                    game.AnswerList.Last().GridPointList.Add(new SchulteGridGame.GridPoint()
                    {
                        X = point.Item1,
                        Y = point.Item2
                    });
                }
            }

            return game;
        }
    }
}
