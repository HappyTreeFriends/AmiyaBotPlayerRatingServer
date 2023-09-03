using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Aliyun.OSS;
using Microsoft.AspNetCore.Hosting;
using System;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Xml.Linq;
using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Hangfire;
using AmiyaBotPlayerRatingServer.Hangfire;

namespace AmiyaBotPlayerRatingServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class CalculateNowController : ControllerBase
    {
        public class AccumulatedCharacterData
        {
            public long Count { get; set; } = 0;
            public double TotalLevel { get; set; } = 0;
            public double TotalSkillLevel { get; set; } = 0;
            public Dictionary<int, (long Count, double Level)> EquipLevel { get; set; } = new Dictionary<int, (long, double)>();
            public Dictionary<string, (long Count, double Level)> SpecializeLevel { get; set; } = new Dictionary<string, (long, double)>();
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

        [HttpGet]
        public object Index()
        {
            var startDate = DateTime.Now.AddDays(-90);
            var endDate = DateTime.Now;
            _backgroundJobClient.Enqueue<CalculateCharacterStatisticsService>(service => service.Calculate(startDate,endDate));
            return Ok();
        }
    }
}
