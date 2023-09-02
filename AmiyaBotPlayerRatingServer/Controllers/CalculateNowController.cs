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
            public double TotalEvolvePhase { get; set; } = 0;
            public double TotalLevel { get; set; } = 0;
            public double TotalSkillLevel { get; set; } = 0;
            public Dictionary<int, (long Count, double Level)> EquipLevel { get; set; } = new Dictionary<int, (long, double)>();
            public Dictionary<string, (long Count, double Level)> SpecializeLevel { get; set; } = new Dictionary<string, (long, double)>();
        }
        
        private readonly IConfiguration _configuration;
        private readonly PlayerRatingDatabaseContext _dbContext;

        public CalculateNowController(IWebHostEnvironment env, IConfiguration configuration, PlayerRatingDatabaseContext dbContext)
        {
            _configuration = configuration;
            _dbContext = dbContext;
        }

        [HttpGet]
        public object Index()
        {
            var startDate = DateTime.Now.AddDays(-60);
            var endDate = DateTime.Now;

            // 从appsettings.json获取OSS相关设置
            string endPoint = _configuration["Aliyun:Oss:EndPoint"]!;
            string bucketName = _configuration["Aliyun:Oss:Bucket"]!;
            string key = _configuration["Aliyun:Oss:Key"]!;
            string secret = _configuration["Aliyun:Oss:Secret"]!;

            // 初始化OSS客户端
            var client = new OssClient(endPoint, key, secret);

            string nextMarker = string.Empty;
            string prefix = "collected_data_v1/"; // Directory prefix

            Dictionary<string, string> latestFiles = new Dictionary<string, string>();

            do
            {
                var listObjectsRequest = new ListObjectsRequest(bucketName)
                {
                    Marker = nextMarker,
                    MaxKeys = 100,
                    Prefix = prefix // Only retrieve objects under the collected_data_v1 directory
                };

                // List objects
                var result = client.ListObjects(listObjectsRequest);

                // File name regular expression
                Regex regex = new Regex(@"chars\.(\w+)\.(\d{8})\.(\d{6})\.json");

                foreach (var summary in result.ObjectSummaries)
                {
                    string fileName = summary.Key;

                    // Use regex to extract date and time information
                    var match = regex.Match(fileName);
                    if (match.Success)
                    {
                        string userId = match.Groups[1].Value;
                        string dateStr = match.Groups[2].Value;
                        string timeStr = match.Groups[3].Value;

                        DateTime fileDate;
                        if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out fileDate))
                        {
                            if (fileDate >= startDate && fileDate <= endDate)
                            {
                                // Check if this file is the latest for this user
                                if (latestFiles.ContainsKey(userId))
                                {
                                    string existingFileName = latestFiles[userId];
                                    var existingMatch = regex.Match(existingFileName);
                                    string existingDateStr = existingMatch.Groups[2].Value;
                                    string existingTimeStr = existingMatch.Groups[3].Value;

                                    if (String.CompareOrdinal(dateStr + timeStr, existingDateStr + existingTimeStr) > 0)
                                    {
                                        // This file is newer
                                        latestFiles[userId] = fileName;
                                    }
                                }
                                else
                                {
                                    latestFiles[userId] = fileName;
                                }
                            }
                        }
                    }
                }

                nextMarker = result.NextMarker;

            } while (!string.IsNullOrEmpty(nextMarker));

            // Now, process the latest file for each user

            Dictionary<string, AccumulatedCharacterData> accumulatedData = new Dictionary<string, AccumulatedCharacterData>();

            foreach (var userId in latestFiles.Keys)
            {
                string latestFile = latestFiles[userId];

                // Read the latest file content
                var stream = client.GetObject(bucketName, latestFile).Content;

                using (StreamReader reader = new StreamReader(stream))
                {
                    string jsonContent = reader.ReadToEnd();

                    // Handle the JSON content
                    JArray characterArray = JArray.Parse(jsonContent);

                    foreach (JObject character in characterArray)
                    {
                        string charId = character["charId"].ToString();
                        int evolvePhase = character["evolvePhase"].ToObject<int>();
                        int level = character["level"].ToObject<int>();
                        int mainSkillLvl = character["mainSkillLvl"].ToObject<int>();

                        // 初始化或获取AccumulatedCharacterData
                        if (!accumulatedData.ContainsKey(charId))
                        {
                            accumulatedData[charId] = new AccumulatedCharacterData();
                        }

                        var data = accumulatedData[charId];

                        // 更新计数和其他统计信息
                        data.Count++;
                        data.TotalEvolvePhase += evolvePhase;
                        data.TotalLevel += level;
                        data.TotalSkillLevel += mainSkillLvl;

                        // 更新平均专精等级
                        JArray skills = (JArray)character["skills"];
                        foreach (JObject skill in skills)
                        {
                            string skillId = skill["id"].ToString();
                            int specializeLevel = skill["specializeLevel"].ToObject<int>();

                            if (!data.SpecializeLevel.ContainsKey(skillId))
                            {
                                data.SpecializeLevel[skillId] = (0, 0);
                            }

                            var (count, totalLevel) = data.SpecializeLevel[skillId];
                            data.SpecializeLevel[skillId] = (count + 1, totalLevel + specializeLevel);
                        }

                        // 更新平均模组等级
                        JArray equips = (JArray)character["equip"];
                        int equipIndex = 0;
                        foreach (JObject equip in equips)
                        {
                            int equipLevel = equip["level"].ToObject<int>();

                            if (!data.EquipLevel.ContainsKey(equipIndex))
                            {
                                data.EquipLevel[equipIndex] = (0, 0);
                            }

                            var (count, totalLevel) = data.EquipLevel[equipIndex];
                            data.EquipLevel[equipIndex] = (count + 1, totalLevel + equipLevel);

                            equipIndex++;
                        }
                    }

                }
            }

            //最后，遍历accumulatedData并计算平均值，然后存入数据库：

            // 删除 CharacterStatistics 表中的所有记录(测试时临时代码)
            _dbContext.CharacterStatistics.RemoveRange(_dbContext.CharacterStatistics);

            // 提交更改以清空表
            _dbContext.SaveChanges();

            foreach (var charId in accumulatedData.Keys)
            {
                var data = accumulatedData[charId];

                // 创建新的 CharacterStatistics 实例
                CharacterStatistics statistics = new CharacterStatistics
                {
                    Id = Guid.NewGuid().ToString(),
                    VersionStart = startDate.ToUniversalTime(),
                    VersionEnd = endDate.ToUniversalTime(),
                    SampleCount = data.Count,
                    CharacterId = charId,
                    AverageEvolvePhase = data.TotalEvolvePhase / data.Count,
                    AverageLevel = data.TotalLevel / data.Count,
                    AverageSkillLevel = data.TotalSkillLevel / data.Count
                };

                // 计算平均专精等级
                List<double> avgSpecializeLevels = new List<double>();
                foreach (var (skillId, (count, totalLevel)) in data.SpecializeLevel)
                {
                    avgSpecializeLevels.Add(totalLevel / (double)count);
                }
                statistics.AverageSpecializeLevel = avgSpecializeLevels;

                // 计算平均模组等级
                Dictionary<int, double> avgEquipLevels = new Dictionary<int, double>();
                foreach (var (equipIndex, (count, totalLevel)) in data.EquipLevel)
                {
                    avgEquipLevels[equipIndex] = totalLevel / (double)count;
                }
                statistics.AverageEquipLevel = avgEquipLevels;

                // 添加到数据库
                _dbContext.CharacterStatistics.Add(statistics);
            }

            _dbContext.SaveChanges();

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
