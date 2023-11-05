using System.Security.Claims;
using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.Model;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmiyaBotPlayerRatingServer.Controllers.MAAControllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MAAConnectionController : ControllerBase
    {
        private readonly PlayerRatingDatabaseContext _context;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILogger<MAAConnectionController> _logger; // 声明一个Logger

        public MAAConnectionController(
            PlayerRatingDatabaseContext context,
            IBackgroundJobClient backgroundJobClient,
            ILogger<MAAConnectionController> logger) // 通过构造函数注入Logger
        {
            _context = context;
            _backgroundJobClient = backgroundJobClient;
            _logger = logger;
        }
        
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpGet("List")]
        public async Task<IActionResult> ListConnections()
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
                // 查询数据库获取该用户的所有MAAConnections
                var connections = await _context.MAAConnections
                    .Where(c => c.UserId == userId&&!String.IsNullOrWhiteSpace(c.Name))
                    .Select(c => new
                    {
                        c.Id,
                        c.DeviceIdentity,
                        c.UserIdentity,
                        c.Name
                    })
                    .ToListAsync();

                return Ok(connections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "列出连接时发生错误。"); // 使用Logger记录错误
                return StatusCode(500, "获取连接列表时发生内部错误。");
            }
        }


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpPost("Initialize")]
        public async Task<IActionResult> InitializeConnection()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("用户未登录。");
            }

            var userId = userIdClaim.Value;

            // 生成UserIdentity
            var userIdentity = Guid.NewGuid().ToString("N");

            var connection = new MAAConnection
            {
                UserId = userId,
                UserIdentity = userIdentity,
                DeviceIdentity = null // 初始时DeviceIdentity为空
            };

            await _context.MAAConnections.AddAsync(connection);
            await _context.SaveChangesAsync();

            // 使用Hangfire创建删除任务
            _backgroundJobClient.Schedule(
                () => DeleteUnconfirmedConnection(connection.Id),
                TimeSpan.FromMinutes(5));

            return Ok(new { connection.Id, connection.UserIdentity });
        }

        [NonAction]
        public async Task DeleteUnconfirmedConnection(Guid connectionId)
        {
            try
            {
                var connection = await _context.MAAConnections.FindAsync(connectionId);
                if (connection != null && string.IsNullOrEmpty(connection.DeviceIdentity))
                {
                    _context.MAAConnections.Remove(connection);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "尝试删除未确认的连接时发生错误，连接ID：{ConnectionId}", connectionId);
            }
        }

        public class CompleteConnectionModel
        {
            public String DeviceIdentity { get; set; }
            public String Name { get; set; }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpPost("Complete/{id}")]
        public async Task<IActionResult> CompleteConnection(Guid id, [FromBody] CompleteConnectionModel model)
        {
            if (string.IsNullOrEmpty(model.DeviceIdentity))
            {
                return BadRequest("DeviceIdentity不能为空。");
            }

            var connection = await _context.MAAConnections.FindAsync(id);

            if (connection == null)
            {
                return NotFound("连接未找到。");
            }

            if (connection.UserId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            {
                return NotFound("连接未找到。");
            }

            // 确认该连接没有被其他设备占用
            var existingConnectionWithDevice = await _context.MAAConnections
                .AnyAsync(c => c.DeviceIdentity == model.DeviceIdentity);
            if (existingConnectionWithDevice)
            {
                return BadRequest("DeviceIdentity已经被占用。");
            }

            // 完成Connection的创建
            connection.DeviceIdentity = model.DeviceIdentity;
            connection.Name = model.Name;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "成功创建连接。" });
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> DeleteConnection(Guid id)
        {
            // 获取当前用户ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("用户未登录。");
            }

            var userId = userIdClaim.Value;

            // 从数据库中找到对应的Connection
            var connectionToDelete = await _context.MAAConnections
                .FirstOrDefaultAsync(c => c.Id == id);

            // 检查是否存在该Connection
            if (connectionToDelete == null)
            {
                return NotFound("连接未找到。");
            }

            // 检查用户是否有权限删除该Connection
            if (connectionToDelete.UserId != userId)
            {
                return NotFound("连接未找到。");
            }

            // 删除该Connection
            _context.MAAConnections.Remove(connectionToDelete);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "成功删除连接。" });
        }

    }

}
