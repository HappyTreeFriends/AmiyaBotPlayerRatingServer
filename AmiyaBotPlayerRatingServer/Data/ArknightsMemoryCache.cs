using Microsoft.Extensions.Caching.Memory;
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
            LoadFile(Path.Combine(Directory.GetCurrentDirectory(), "Resources", "CharacterMap.json"), "character_table.json");
            LoadFile(Path.Combine(Directory.GetCurrentDirectory(), "Resources", "SkillMap.json"), "skill_table.json");
            LoadFile(Path.Combine(Directory.GetCurrentDirectory(), "Resources", "SkinTable.json"), "skin_table.json");
        }

        private void LoadFile(String filePath, String resKey)
        {
            if (File.Exists(filePath))
            {
                var fileStream = new FileStream(filePath, FileMode.Open);
                using var reader = new StreamReader(fileStream);
                var text = reader.ReadToEnd();
                var characterMap = JToken.Parse(text);
                _cache.Set("JToken:"+resKey, characterMap);
                _cache.Set("Text:"+resKey, text);

            }
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
