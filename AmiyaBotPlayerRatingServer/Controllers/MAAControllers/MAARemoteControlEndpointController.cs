using AmiyaBotPlayerRatingServer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AmiyaBotPlayerRatingServer.Model;
using Microsoft.AspNetCore.Authorization;

namespace AmiyaBotPlayerRatingServer.Controllers.MAAControllers
{
    [ApiController]
    [Route("api/maa")]
    public class MAARemoteControlEndpointController : ControllerBase
    {
        private readonly PlayerRatingDatabaseContext _context;
        private readonly ILogger<MAARemoteControlEndpointController> _logger;

        public MAARemoteControlEndpointController(PlayerRatingDatabaseContext context, ILogger<MAARemoteControlEndpointController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // DTO定义
        public class MAARequest
        {
            public string User { get; set; }
            public string Device { get; set; }
        }

        public class MAATaskDto
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public string? Params { get; set; } // 可选参数
        }

        // POST: maa/getTask
        [HttpPost("getTask")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTasks([FromBody] MAARequest request)
        {
            // 从数据库获取任务列表
            // 需要根据用户标识符和设备标识符来获取任务列表
            // 先获取连接

            var connection = await _context.MAAConnections
                .FirstOrDefaultAsync(c => c.UserIdentity == request.User && c.DeviceIdentity == request.Device);

            if (connection == null)
            {
                return NotFound("该连接配置不存在");
            }
            
            // 数据库中获取最近五分钟未完成的任务
            var tasks = _context.MAATasks.Where(t => t.ConnectionId == connection.Id && t.IsCompleted)
                .Where(t=>t.CreatedAt>DateTime.Now.AddMinutes(-5))
                .OrderByDescending(t => t.CreatedAt).Select(t => new MAATaskDto
            {
                Id = t.Id.ToString("N"),
                Type = t.Type,
                Params = t.Parameters
            });

            // 检查任务列表是否为空
            if (!tasks.Any())
            {
                return Ok(new { tasks = new List<MAATaskDto>() });
            }

            return Ok(new { tasks = tasks });
        }

        public class TaskReportModel
        {
            public string User { get; set; }
            public string Device { get; set; }
            public string Task { get; set; }
            public string Status { get; set; }
            public string Payload { get; set; }
        }

        // POST: maa/reportStatus
        [HttpPost("reportStatus")]
        [AllowAnonymous]
        public async Task<IActionResult> ReportTaskStatus([FromBody] TaskReportModel request)
        {
            try
            {
                var connection = await _context.MAAConnections
                    .FirstOrDefaultAsync(c => c.UserIdentity == request.User && c.DeviceIdentity == request.Device);

                if (connection == null)
                {
                    return Ok();
                }

                var taskId = Guid.Parse(request.Task);
                var task = await _context.MAATasks.FirstOrDefaultAsync(t => t.Id == taskId && t.ConnectionId == connection.Id);

                if (task == null)
                {
                    return Ok();
                }

                var result = new MAAResponse
                {
                    TaskId = task.Id,
                    Payload = request.Payload
                };

                _context.MAAResponses.Add(result);

                task.IsCompleted = true;
                task.CompletedAt = DateTime.Now;

                _context.MAATasks.Update(task);

                await _context.SaveChangesAsync();

                // 如果一切顺利，返回200 OK
                return Ok();
            }
            catch (Exception ex)
            {
                // 出现异常，返回500服务器错误
                // 在实际的生产环境中，应该记录异常信息而不是直接返回
                return StatusCode(500, "服务器内部错误：" + ex.Message);
            }
        }
    }

}
