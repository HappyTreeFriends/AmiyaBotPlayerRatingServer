using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Threading;
using StackExchange.Redis;

namespace AmiyaBotPlayerRatingServer.Data
{
    public partial class ArknightsMemoryCache
    {
        
        private readonly IMemoryCache _cache;
        private readonly ILogger<ArknightsMemoryCache> _logger;

        public ArknightsMemoryCache(IMemoryCache memoryCache,ILogger<ArknightsMemoryCache> logger)
        {
            _cache = memoryCache;
            _logger = logger;

            Task.Run(() =>
            {
                try
                {
                    InitializeAssets();
                    UpdateCache();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize assets");
                }

            });

        }


        public void UpdateCache()
        {
            //加载所有Json文件
            LoadJsonFromDir(Path.Combine(_directoryPath, "gamedata/excel"));
            LoadJsonFromDir(Path.Combine(_directoryPath, "indexes"));

            //生成CustomFile
            GenerateCustomFiles();

            //生成其他Class需要的Data
            SchulteGridGameData.LoadCharacterMap(this);
        }

    }
}
