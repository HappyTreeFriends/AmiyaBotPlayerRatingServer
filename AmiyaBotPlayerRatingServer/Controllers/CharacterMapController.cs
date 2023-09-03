using AmiyaBotPlayerRatingServer.Data;
using Microsoft.AspNetCore.Mvc;

namespace AmiyaBotPlayerRatingServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class CharacterMapController : ControllerBase
    {
        private readonly PlayerRatingDatabaseContext _dbContext;

        public CharacterMapController(PlayerRatingDatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

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