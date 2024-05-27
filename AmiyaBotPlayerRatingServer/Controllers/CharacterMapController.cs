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

        private static Dictionary<String,String> _operatorIdsCache = new();
        private static DateTime _operatorIdsCacheLastUpdate = DateTime.MinValue;

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
        
    }
}