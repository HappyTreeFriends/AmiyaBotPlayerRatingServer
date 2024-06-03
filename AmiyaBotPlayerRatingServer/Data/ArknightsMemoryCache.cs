using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Threading;

namespace AmiyaBotPlayerRatingServer.Data
{
    public class ArknightsMemoryCache
    {
        // abs path is /app/Resources
        private readonly string _directoryPath = "Resources/amiya-bot-assets";
        private readonly string _gitRepoUrl = "https://gitee.com/amiya-bot/amiya-bot-assets.git";
        private readonly string _zipFilePath = "Resources/amiya-bot-assets/gamedata.zip";
        private readonly string _extractPath = "Resources/amiya-bot-assets/gamedata";


        public class ArknightsOperator
        {
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

        public string Id { get; set; } = string.Empty;
        public Dictionary<string, object> Cv { get; set; } = new Dictionary<string, object>();

        public string Type { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        public string Range { get; set; } = string.Empty;
        public int Rarity { get; set; } = 0;
        public string Number { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string EnName { get; set; } = string.Empty;
        public string WikiName { get; set; } = string.Empty;
        public string IndexName { get; set; } = string.Empty;
        public string OriginName { get; set; } = string.Empty;

        public string Classes { get; set; } = string.Empty;
        public string ClassesSub { get; set; } = string.Empty;
        public string ClassesCode { get; set; } = string.Empty;

        public string Sex { get; set; } = string.Empty;
        public string Race { get; set; } = string.Empty;
        public string Drawer { get; set; } = string.Empty;
        public string TeamId { get; set; } = string.Empty;
        public string Team { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string NationId { get; set; } = string.Empty;
        public string Nation { get; set; } = string.Empty;
        public string Birthday { get; set; } = string.Empty;

        public string Profile { get; set; } = string.Empty;
        public string Impression { get; set; } = string.Empty;
        public string PotentialItem { get; set; } = string.Empty;

        public bool Limit { get; set; } = false;
        public bool Unavailable { get; set; } = false;
        public bool IsRecruit { get; set; } = false;
        public bool IsClassic { get; set; } = false;
        public bool IsSp { get; set; } = false;

        //public abstract Dictionary<string, string> Dict();
        //public abstract Tuple<Dictionary<string, string>, Dictionary<string, string>> Detail();
        //public abstract List<Dictionary<string, string>> Tokens();
        //public abstract List<Dictionary<string, string>> Talents();
        //public abstract List<Dictionary<string, string>> Potential();
        //public abstract List<Dictionary<string, string>> EvolveCosts();
        //public abstract Tuple<List<Dictionary<string, string>>, List<string>, List<Dictionary<string, string>>, Dictionary<string, List<Dictionary<string, string>>>> Skills();
        //public abstract List<Dictionary<string, string>> BuildingSkills();
        //public abstract List<Dictionary<string, string>> Voices();
        //public abstract List<Dictionary<string, string>> Stories();
        //public abstract List<Dictionary<string, string>> Skins();
        //public abstract List<Dictionary<string, string>> Modules();
    }
        
        private readonly IMemoryCache _cache;
        private readonly ILogger<ArknightsMemoryCache> _logger;
        private readonly CancellationToken cancellationToken = new CancellationToken();

        public ArknightsMemoryCache(IMemoryCache memoryCache,ILogger<ArknightsMemoryCache> logger)
        {
            _cache = memoryCache;
            _logger = logger;

            Task.Run(() =>
            {
                try
                {
                    InitializeAssets();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize assets");
                }

            }, cancellationToken);

            //不可以用Hangfire，因为每个服务器都会有一个实例
            _ = new Timer(UpdateCache, null, TimeSpan.Zero, TimeSpan.FromHours(1));
            UpdateCache(null);
        }

        private void InitializeAssets()
        {
            //输出目录绝对路径
            var dir = new DirectoryInfo(_directoryPath);
            _logger.LogInformation($"Directory Path: {dir.FullName}");


            if (!Directory.Exists(_directoryPath))
            {
                Directory.CreateDirectory(_directoryPath);
                CloneRepo();
                ExtractGameData();
            }
            else
            {
                if (!IsGitRepo())
                {
                    Directory.Delete(_directoryPath, true);
                    Directory.CreateDirectory(_directoryPath);
                    CloneRepo();
                }
                else
                {
                    PullRepo();
                }
                ExtractGameData();
            }

            _logger.LogInformation("InitializeAssets Completed");
        }

        private void CloneRepo()
        {
            ExecuteShellCommand($"git clone {_gitRepoUrl} {_directoryPath}");
        }

        private void PullRepo()
        {
            ExecuteShellCommand($"cd {_directoryPath} && git pull");
        }

        private bool IsGitRepo()
        {
            return Directory.Exists(Path.Combine(_directoryPath, ".git"));
        }

        private void ExtractGameData()
        {
            if (Directory.Exists(_extractPath))
            {
                Directory.Delete(_extractPath, true);
            }
            ZipFile.ExtractToDirectory(_zipFilePath, _extractPath);
        }

        private void ExecuteShellCommand(string command)
        {
            var processInfo = new ProcessStartInfo("bash", $"-c \"{command}\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();
            process.WaitForExit();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void UpdateAssets()
        {
            PullRepo();
            ExtractGameData();
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
