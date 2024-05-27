using Microsoft.AspNetCore.Mvc;
using AmiyaBotPlayerRatingServer.Data;
using Hangfire;
using AmiyaBotPlayerRatingServer.Hangfire;
using Microsoft.AspNetCore.Authorization;

namespace AmiyaBotPlayerRatingServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CalculateNowController : ControllerBase
    {
        public class AccumulatedCharacterData
        {
            public long Count { get; set; } = 0;
            public double TotalLevel { get; set; } = 0;
            public double TotalSkillLevel { get; set; } = 0;
            public Dictionary<int, (long Count, double Level)> EquipLevel { get; set; } = new();
            public Dictionary<string, (long Count, double Level)> SpecializeLevel { get; set; } = new();
        }
        
        private readonly IConfiguration _configuration;
        private readonly PlayerRatingDatabaseContext _dbContext;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public CalculateNowController(IConfiguration configuration, PlayerRatingDatabaseContext dbContext, IBackgroundJobClient backgroundJobClient)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _backgroundJobClient = backgroundJobClient;
        }

        [Authorize(Roles = "管理员账户")]
        [HttpGet]
        public object Index()
        {
            var startDate = DateTime.Now.AddDays(-180);
            var endDate = DateTime.Now;
            _backgroundJobClient.Enqueue<CalculateCharacterStatisticsService>(service => service.Calculate(startDate,endDate));
            return Ok();
        }
    }
}
