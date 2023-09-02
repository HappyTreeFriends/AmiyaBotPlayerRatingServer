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
            // 从数据库中获取最新的统计数据
            var stats = _dbContext.CharacterStatistics.ToList()  // 先获取数据
                .Select(s => new {
                    s.Id,
                    s.VersionStart,
                    s.VersionEnd,
                    s.SampleCount,
                    s.CharacterId,
                    AverageEvolvePhase = Math.Round(s.AverageEvolvePhase, 2),
                    AverageLevel = Math.Round(s.AverageLevel, 2),
                    AverageSkillLevel = Math.Round(s.AverageSkillLevel, 2),
                    AverageSpecializeLevel = s.AverageSpecializeLevel.Select(x => Math.Round(x, 2)).ToList(),
                    AverageEquipLevel = s.AverageEquipLevel.ToDictionary(kvp => kvp.Key, kvp => Math.Round(kvp.Value, 2))
                })
                .ToList();



            // 返回统计数据的 JSON 表示形式
            return new JsonResult(stats);
        }
    }
}