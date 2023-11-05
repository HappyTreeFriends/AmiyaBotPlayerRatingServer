using AmiyaBotPlayerRatingServer.Data;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace AmiyaBotPlayerRatingServer.Controllers.MAAControllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MAATaskController : ControllerBase
    {
        private readonly PlayerRatingDatabaseContext _context;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILogger<MAATaskController> _logger; // 声明一个Logger

        public MAATaskController(
            PlayerRatingDatabaseContext context,
            IBackgroundJobClient backgroundJobClient,
            ILogger<MAATaskController> logger) // 通过构造函数注入Logger
        {
            _context = context;
            _backgroundJobClient = backgroundJobClient;
            _logger = logger;
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpGet("GetConnectionLatestScreenShot/{id}")]
        public async Task<IActionResult> GetConnectionLatestScreenShot(Guid id)
        {
            // 从JWT中提取用户ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("用户未登录。");
            }

            var userId = userIdClaim.Value;

            try
            {
                var connection = _context.MAAConnections.FirstOrDefault(c => c.Id == id&&c.UserId == userId);

                if (connection == null)
                {
                    return NotFound("指定的连接不存在。");
                }

                var latestScreenShot = await _context.MAATasks
                    .Where(s => s.ConnectionId == id&&s.IsCompleted==true&&(s.Type=="CaptureImage"||s.Type=="CaptureImageNow"))
                    .FirstOrDefaultAsync();

                if (latestScreenShot == null)
                {
                    return Ok(new
                    {
                        Image=""
                    });
                }

                var result = _context.MAAResponses.FirstOrDefault(r => r.TaskId == latestScreenShot.Id);

                if (result == null)
                {
                    return Ok(new
                    {
                        Image = ""
                    });
                }


                return Ok(new
                {
                    Image = result.Payload
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "列出连接时发生错误。"); // 使用Logger记录错误
                return StatusCode(500, "获取连接列表时发生内部错误。");
            }
        }

    }
}
