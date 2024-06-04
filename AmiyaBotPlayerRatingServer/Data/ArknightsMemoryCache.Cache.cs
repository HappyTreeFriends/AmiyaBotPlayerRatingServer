using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace AmiyaBotPlayerRatingServer.Data
{
    public partial class ArknightsMemoryCache
    {
        private readonly Dictionary<String,DateTime> _lastModified = new();

        private void LoadFile(String filePath, String resKey)
        {
            if (File.Exists(filePath))
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var reader = new StreamReader(fileStream);
                var text = reader.ReadToEnd();
                var characterMap = JToken.Parse(text);
                _cache.Set("JToken:" + resKey, characterMap);
                _cache.Set("Text:" + resKey, text);
                _lastModified[resKey] = File.GetLastWriteTime(filePath);
            }
        }

        private void LoadJson(JToken json, String key)
        {
            _cache.Set("JToken:" + key, json);
            _cache.Set("Text:" + key, json.ToString());
            _lastModified[key] = DateTime.Now;
        }

        public void LoadObject(Object obj, String key)
        {
            var jsonText = JsonConvert.SerializeObject(obj);
            var jsonObj = JsonConvert.DeserializeObject(jsonText);
            _cache.Set("JToken:" + key, jsonObj);
            _cache.Set("Text:" + key, jsonText);
            _lastModified[key] = DateTime.Now;
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

        public T? GetObject<T>(String key)
        {
            if (_cache.TryGetValue<JToken>("JToken:" + key, out var value))
            {
                if (value == null)
                {
                    return default;
                }
                var obj = value.ToObject<T>();
                return obj;
            }
            return default;
        }

        public Dictionary<String,DateTime> GetKeys()
        {
            return _lastModified;
        }
    }
}
