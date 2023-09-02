using AmiyaBotPlayerRatingServer.Data;
using Microsoft.AspNetCore.Mvc;

namespace AmiyaBotPlayerRatingServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class CharacterStatisticsController : ControllerBase
    {
        private readonly PlayerRatingDatabaseContext _dbContext;

        public CharacterStatisticsController(PlayerRatingDatabaseContext dbContext)
        {
            _dbContext = dbContext;
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
                .Select(s => new {
                    // Removed s.Id
                    s.SampleCount,
                    s.CharacterId,
                    AverageEvolvePhase = Math.Round(s.AverageEvolvePhase, 2),
                    AverageLevel = Math.Round(s.AverageLevel, 2),
                    AverageSkillLevel = Math.Round(s.AverageSkillLevel, 2),
                    AverageSpecializeLevel = s.AverageSpecializeLevel.Select(x => Math.Round(x, 2)).ToList(),
                    AverageEquipLevel = s.AverageEquipLevel.ToDictionary(kvp => kvp.Key, kvp => Math.Round(kvp.Value, 2))
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