using AmiyaBotPlayerRatingServer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;

namespace AmiyaBotPlayerRatingServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CharacterMapController : ControllerBase
    {
        private readonly PlayerRatingDatabaseContext _dbContext;

        private static Dictionary<String,String> operatorIdsCache = new Dictionary<string, string>();
        private static DateTime operatorIdsCacheLastUpdate = DateTime.MinValue;

        public CharacterMapController(PlayerRatingDatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        [AllowAnonymous]
        [HttpGet]
        public object Index()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "CharacterMap.json");

            // 从磁盘读取JSON文件
            if (System.IO.File.Exists(filePath))
            {
                var fileStream = new FileStream(filePath, FileMode.Open);
                return new FileStreamResult(fileStream, "application/json");
            }

            return NotFound("JSON file not found");
        }

        [AllowAnonymous]
        [HttpGet("/operator-ids")]
        public object OperatorIds()
        {
            if (DateTime.Now - operatorIdsCacheLastUpdate < new TimeSpan(1, 0, 0))
            {
                return Ok(operatorIdsCache);
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "OperatorIds.json");

            // 从磁盘读取JSON文件
            if (System.IO.File.Exists(filePath))
            {
                var fileStream = new FileStream(filePath, FileMode.Open);
                using var reader = new StreamReader(fileStream);
                var characterMap = JObject.Parse(reader.ReadToEnd());

                var tmpOperatorIdsCache = new Dictionary<string, string>();
                foreach (var op in characterMap)
                {
                    var opName = op.Value?["name"]?.ToString();
                    if (opName != null)
                    {
                        tmpOperatorIdsCache.Add(op.Key, opName);
                    }
                }

                operatorIdsCache = tmpOperatorIdsCache;
                operatorIdsCacheLastUpdate = DateTime.Now;
                return Ok(operatorIdsCache);

            }

            return NotFound("JSON file not found");
        }

    }
}