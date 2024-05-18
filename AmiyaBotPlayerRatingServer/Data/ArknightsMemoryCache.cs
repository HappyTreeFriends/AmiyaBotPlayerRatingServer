using Hangfire.Common;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AmiyaBotPlayerRatingServer.Data
{
    public class ArknightsMemoryCache
    {
        private readonly IMemoryCache _cache;
        private readonly Timer _timer;

        public ArknightsMemoryCache(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
            _timer = new Timer(UpdateCache, null, TimeSpan.Zero, TimeSpan.FromHours(1));
            UpdateCache(null);
        }

        private void UpdateCache(object? state)
        {
            LoadFile(Path.Combine(Directory.GetCurrentDirectory(), "Resources/gamedata", "character_table.json"), "character_table.json");
            LoadFile(Path.Combine(Directory.GetCurrentDirectory(), "Resources/gamedata", "skill_table.json"), "skill_table.json");
            LoadFile(Path.Combine(Directory.GetCurrentDirectory(), "Resources/gamedata", "skin_table.json"), "skin_table.json");
            LoadFile(Path.Combine(Directory.GetCurrentDirectory(), "Resources/gamedata", "skinUrls.json"), "skinUrls.json");

            try
            {
                //进行一点点逻辑处理
                var characterTable = JsonConvert.DeserializeObject<JToken>(GetText("character_table.json")!) as JObject;
                var character_names = new JObject();

                foreach (var chara in characterTable)
                {
                    chara.Value["charId"] = chara.Key;
                    var obtain = chara.Value["itemObtainApproach"]?.ToString();
                    if (obtain != "凭证交易所" && obtain != "招募寻访" &&
                        obtain != "活动获得" && obtain != "主线剧情" &&
                        obtain != "信用交易所")
                    {
                        continue;
                    }
                    character_names[chara.Key] = chara.Value["name"];
                }

                LoadJson(character_names, "character_names.json");


                var skillTable = JsonConvert.DeserializeObject<JToken>(GetText("skill_table.json")!) as JObject;
                var skillDict = new Dictionary<String, JToken>();
                foreach (var skillObj in skillTable!)
                {
                    var key = skillObj.Key;
                    var value = skillObj.Value;
                    skillDict.Add(key, value);
                }

                foreach (var charaObj in characterTable!)
                {
                    var value = charaObj.Value;
                    var skills = value["skills"] as JArray;
                    foreach (var skill in skills)
                    {
                        var skillId = skill["skillId"]?.ToString();
                        var skillData = skillDict.GetValueOrDefault(skillId);
                        if (skillData!=null)
                        {
                            skill["skillData"] = skillData;
                        }
                    }
                }

                var skinTable = JsonConvert.DeserializeObject<JToken>(GetText("skin_table.json")!) as JObject;
                foreach (var skinObj in (skinTable!["charSkins"] as JObject)!)
                {
                    var key = skinObj.Key;
                    var value = skinObj.Value!;
                    //value!["skinId"] = key;
                    var charId = value["charId"]?.ToString()!;

                    var chara = characterTable[charId];
                    if (chara!=null)
                    {
                        var skin = chara["skins"] as JArray;
                        if (skin == null)
                        {
                            skin = new JArray();
                            chara["skins"] = skin;
                        }
                        skin.Add(value);
                    }
                }



                LoadJson(characterTable,"character_table_full.json");
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
