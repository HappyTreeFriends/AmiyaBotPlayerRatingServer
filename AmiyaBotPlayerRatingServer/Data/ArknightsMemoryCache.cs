using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AmiyaBotPlayerRatingServer.Data
{
    public class ArknightsMemoryCache
    {
        private readonly IMemoryCache _cache;

        public ArknightsMemoryCache(IMemoryCache memoryCache)
        {
            _cache = memoryCache;

            //不可以用Hangfire，因为每个服务器都会有一个实例
            _ = new Timer(UpdateCache, null, TimeSpan.Zero, TimeSpan.FromHours(1));
            UpdateCache(null);
        }

        private void UpdateCache(object? state)
        {
            LoadFile(Path.Combine(Directory.GetCurrentDirectory(), "Resources/gamedata", "character_table.json"), "character_table.json");
            LoadFile(Path.Combine(Directory.GetCurrentDirectory(), "Resources/gamedata", "skill_table.json"), "skill_table.json");
            LoadFile(Path.Combine(Directory.GetCurrentDirectory(), "Resources/gamedata", "skin_table.json"), "skin_table.json");
            LoadFile(Path.Combine(Directory.GetCurrentDirectory(), "Resources/gamedata", "skinUrls.json"), "skinUrls.json");

            GenerateCustomFiles();

            SchulteGridGameData.LoadCharacterMap(this);
        }

        private void GenerateCustomFiles()
        {
            try
            {
                //进行一点点逻辑处理
                var characterTable = JsonConvert.DeserializeObject<JToken>(GetText("character_table.json")!) as JObject;

                if (characterTable == null)
                {
                    return;
                }

                var characterNames = new JObject();

                var newCharacterTable = new JObject();

                foreach (var character in characterTable)
                {
                    if (character.Value == null)
                    {
                        continue;
                    }

                    var obtain = character.Value["itemObtainApproach"]?.ToString();
                    if (obtain != "凭证交易所" && obtain != "招募寻访" &&
                        obtain != "活动获得" && obtain != "主线剧情" &&
                        obtain != "信用交易所")
                    {
                        continue;
                    }
                    character.Value["charId"] = character.Key;
                    characterNames[character.Key] = character.Value["name"];
                    newCharacterTable[character.Key] = character.Value;
                }

                LoadJson(characterNames, "character_names.json");

                characterTable = newCharacterTable;

                var skillTable = JsonConvert.DeserializeObject<JToken>(GetText("skill_table.json")!) as JObject;
                var skillDict = new Dictionary<String, JToken>();
                foreach (var skillObj in skillTable!)
                {
                    var key = skillObj.Key;
                    var value = skillObj.Value!;
                    skillDict.Add(key, value);
                }

                foreach (var character in characterTable)
                {
                    var value = character.Value;
                    var skills = value!["skills"] as JArray;
                    if(skills == null) continue;
                    foreach (var skill in skills)
                    {
                        var skillId = skill["skillId"]?.ToString();
                        if (skillId == null)
                        {
                            continue;
                        }
                        var skillData = skillDict.GetValueOrDefault(skillId);
                        if (skillData != null)
                        {
                            skill["skillData"] = skillData;
                        }
                    }
                }

                var skinTable = JsonConvert.DeserializeObject<JToken>(GetText("skin_table.json")!) as JObject;
                foreach (var skinObj in (skinTable!["charSkins"] as JObject)!)
                {
                    var value = skinObj.Value!;
                    //value!["skinId"] = key; 该对象已有skinId
                    var charId = value["charId"]?.ToString()!;
                    
                    var character = characterTable[charId];
                    if (character != null)
                    {
                        var skin = character["skins"] as JArray;
                        if (skin == null)
                        {
                            skin = new JArray();
                            character["skins"] = skin;
                        }
                        skin.Add(value);
                    }
                }
                
                LoadJson(characterTable, "character_table_full.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void LoadFile(String filePath, String resKey)
        {
            if (File.Exists(filePath))
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var reader = new StreamReader(fileStream);
                var text = reader.ReadToEnd();
                var characterMap = JToken.Parse(text);
                _cache.Set("JToken:"+resKey, characterMap);
                _cache.Set("Text:"+resKey, text);

            }
        }

        private void LoadJson(JToken json, String key)
        {
            _cache.Set("JToken:" + key, json);
            _cache.Set("Text:" + key, json.ToString());
        }

        public void LoadObject(Object obj, String key)
        {
            var jsonText = JsonConvert.SerializeObject(obj);
            var jsonObj = JsonConvert.DeserializeObject(jsonText);
            _cache.Set("JToken:" + key, jsonObj);
            _cache.Set("Text:" + key, jsonText);
        }

        public JToken? GetJson(String key)
        {
            if (_cache.TryGetValue<JToken>("JToken:" + key, out var value))
            {
                return value;
            }
            return null;
        }

        public String? GetText(String key)
        {
            if (_cache.TryGetValue<String>("Text:" + key, out var value))
            {
                return value;
            }
            return null;
        }
    }
}
