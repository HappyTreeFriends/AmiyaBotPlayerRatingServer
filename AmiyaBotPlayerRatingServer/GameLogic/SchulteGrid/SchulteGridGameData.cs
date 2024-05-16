using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid
{
    public static class SchulteGridGameData
    {
        private static JObject? _characterMap;
        private static Dictionary<string, Dictionary<string, Object>>? _nameDicts;
        private static List<String> operatorList = new List<string>();
        private static JObject? _skillMap;

        private static void LoadCharacterMap()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "CharacterMap.json");

            if (System.IO.File.Exists(filePath))
            {
                var fileStream = new FileStream(filePath, FileMode.Open);
                using var reader = new StreamReader(fileStream);
                _characterMap = JObject.Parse(reader.ReadToEnd());
            }

            filePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "SkillMap.json");

            if (System.IO.File.Exists(filePath))
            {
                var fileStream = new FileStream(filePath, FileMode.Open);
                using var reader = new StreamReader(fileStream);
                _skillMap = JObject.Parse(reader.ReadToEnd());
            }

            if (_characterMap != null)
            {
                foreach (var op in _characterMap)
                {
                    var opName = op.Value?["name"]?.ToString();
                    if(opName != null)
                        operatorList.Add(opName);
                }
            }

            _nameDicts = new Dictionary<string, Dictionary<string, Object>>
            {
                { "skill", BuildNameDict(GetSkills, "skill_name") },
                //{ "talent", BuildNameDict(GetTalents, "talent_name") },
                // { "equip", BuildNameDict(GetEquipments, "uniEquipName") }
            };
        }

        // Simulate methods to get skills, talents, and equipment from an operator JObject
        private static List<String> GetSkills(JObject op)
        {
            // Assuming there is a "skills" array in the operator's JObject
            var skills = new List<String>();
            if (op["skills"] is JArray skillsArray)
            {
                foreach (var skill in skillsArray)
                {
                    var skillId = skill["skillId"].ToString();
                    if (_skillMap[skillId] is JObject skillData)
                    {
                        skills.Add(skillData["levels"][0]["name"].ToString());
                    }
                }
            }
            return skills;
        }

        public static List<String> GetTalents(JObject op)
        {
            // Assuming there is a "talents" array in the operator's JObject
            var talents = new List<String>();
            if (op["talents"] is JArray talentsArray)
            {
                foreach (var talent in talentsArray)
                {
                    talents.Add((String)talent);
                }
            }
            return talents;
        }

        public static List<String> GetEquipments(JObject op)
        {
            // Assuming there is a "equipment" array in the operator's JObject
            var equipments = new List<String>();
            if (op["equipments"] is JArray equipmentsArray)
            {
                foreach (var equipment in equipmentsArray)
                {
                    equipments.Add((String)equipment);
                }
            }
            return equipments;
        }

        private static Dictionary<string, object> BuildNameDict(
            Func<JObject, List<String>> dataFunction, string keyName)
        {
            Dictionary<string, string> tempDict = new Dictionary<string, string>();
            List<string> nameKeys = new List<string>();
            Dictionary<string, string> dataDict = new Dictionary<string, string>();
            List<string> blackList = new List<string>();

            var operators = new Dictionary<string, JObject>();
            if (_characterMap != null)
            {
                foreach (var op in _characterMap)
                {
                    operators.Add(op.Key, (JObject)op.Value);
                }
            }

            foreach (var op in operators)
            {
                var obtain = op.Value["itemObtainApproach"]?.ToString();
                if (obtain != "凭证交易所" && obtain != "招募寻访" && 
                    obtain != "活动获得" && obtain != "主线剧情" && 
                    obtain != "信用交易所")
                {
                    continue;
                }

                var items = dataFunction(op.Value);
                foreach (var item in items)
                {
                    string itemName = Regex.Replace(item, @"[^\w]", "");
                    tempDict[itemName] = op.Value["name"].ToString();
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

        static SchulteGridGameData()
        {
            LoadCharacterMap();
        }

        public static bool IsOperator(String operatorName)
        {
            return operatorList.Contains(operatorName);
        }

        public static async Task<SchulteGridGame> BuildContinuousMode()
        {
            var wordType = "skill";
            var namedictObject = _nameDicts[wordType];
            var wordsMap = ((Dictionary<string, string>)namedictObject["data"]);
            var words = wordsMap.Keys.ToList();
            var blackList = (List<string>)namedictObject["blackList"];

            var (puzzle,answer) = await SchulteGridContinuousGameBuilder.BuildPuzzleContinuousMode(10, 10, words, blackList,3);

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

        public static Dictionary<string, Dictionary<string, Object>>? DebugGetNameDict()
        {
            return _nameDicts;
        }
    }
}
