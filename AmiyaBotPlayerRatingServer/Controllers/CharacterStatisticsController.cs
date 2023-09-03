using AmiyaBotPlayerRatingServer.Data;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace AmiyaBotPlayerRatingServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class CharacterStatisticsController : ControllerBase
    {
        private readonly PlayerRatingDatabaseContext _dbContext;
        private readonly JObject _characterMap;

        public CharacterStatisticsController(PlayerRatingDatabaseContext dbContext)
        {
            _dbContext = dbContext;

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "CharacterMap.json");
            string fileContent = System.IO.File.ReadAllText(filePath);
            _characterMap = JObject.Parse(fileContent);
        }

        public (int evolvePhase, int level) ReverseCalculateLevel(double calculatedLevel, string charId)
        {
            var charData = _characterMap[charId];
            var rarity = charData["rarity"].ToObject<int>();
            int baseIncrease = 0;
            int evolveIncrease = 0;

            // 根据rarity来设定 baseIncrease 和 evolveIncrease
            switch (rarity)
            {
                case 5:
                    baseIncrease = 50;
                    evolveIncrease = 80;
                    break;
                case 4:
                    baseIncrease = 50;
                    evolveIncrease = 70;
                    break;
                case 3:
                    baseIncrease = 45;
                    evolveIncrease = 60;
                    break;
                case 2:
                    baseIncrease = 40;
                    break;
            }

            // 判断evolvePhase和level
            if (calculatedLevel >= baseIncrease + evolveIncrease&&rarity>=3)
            {
                return (2, (int)(calculatedLevel - baseIncrease - evolveIncrease));
            }
            else if (calculatedLevel >= baseIncrease && rarity >= 2)
            {
                return (1, (int)(calculatedLevel - baseIncrease));
            }
            else
            {
                return (0, (int)calculatedLevel);
            }
        }



        [HttpGet]
        public object Index()
        {
            var statsList = _dbContext.CharacterStatistics.ToList();

            if (statsList.Count == 0)
            {
                return new JsonResult(new { message = "No statistics available" });
            }

            var firstStat = statsList.First();

            // 先获取统计数据
            var stats = statsList
                .Select(s => {
                    var (AverageEvolvePhase, AverageLevel) = ReverseCalculateLevel(s.AverageLevel, s.CharacterId);
                    return new
                    {
                        // Removed s.Id
                        s.SampleCount,
                        s.CharacterId,
                        AverageEvolvePhase,
                        AverageLevel,
                        AverageCalculatedLevel = s.AverageLevel,
                        AverageSkillLevel = Math.Round(s.AverageSkillLevel, 2),
                        AverageSpecializeLevel = s.AverageSpecializeLevel.Select(x => Math.Round(x, 2)).ToList(),
                        AverageEquipLevel = s.AverageEquipLevel.ToDictionary(kvp => kvp.Key, kvp => Math.Round(kvp.Value, 2))
                    };
                })
                .ToList();

            // 返回统计数据的 JSON 表示形式，将 VersionStart, VersionEnd 和 BatchCount 提取到外层
            return new JsonResult(new
            {
                VersionStart = firstStat.VersionStart,
                VersionEnd = firstStat.VersionEnd,
                BatchCount = firstStat.BatchCount,
                Data = stats
            });
        }
    }
}