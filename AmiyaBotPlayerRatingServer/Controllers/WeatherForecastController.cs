using AmiyBotPlayerRatingServer;
using Microsoft.AspNetCore.Mvc;

namespace AmiyaBotPlayerRatingServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class BlankController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public BlankController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet]
        public object Index()
        {
            return new { _env.EnvironmentName };
        }
    }
}