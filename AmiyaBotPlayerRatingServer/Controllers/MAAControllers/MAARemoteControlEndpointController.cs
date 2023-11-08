using AmiyaBotPlayerRatingServer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmiyaBotPlayerRatingServer.Model;
using Microsoft.AspNetCore.Authorization;
using Hangfire;
using AmiyaBotPlayerRatingServer.Hangfire;

namespace AmiyaBotPlayerRatingServer.Controllers.MAAControllers
{
    [ApiController]
    [Route("api/maa")]
    public class MAARemoteControlEndpointController : ControllerBase
    {
        private readonly PlayerRatingDatabaseContext _context;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILogger<MAARemoteControlEndpointController> _logger;

        public MAARemoteControlEndpointController(PlayerRatingDatabaseContext context,
            IBackgroundJobClient backgroundJobClient,
            ILogger<MAARemoteControlEndpointController> logger)
        {
            _context = context;
            _backgroundJobClient = backgroundJobClient;
            _logger = logger;
        }

        #region Data Objects

        #pragma warning disable CS8618
        // ReSharper disable UnusedAutoPropertyAccessor.Global
        public class MAARequest
        {
            public string User { get; set; }
            public string Device { get; set; }
        }
        public class TaskReportModel
        {
            public string User { get; set; }
            public string Device { get; set; }
            public string Task { get; set; }
            // ReSharper disable once UnusedMember.Global
            public string Status { get; set; }
            public string Payload { get; set; }
        }
        // ReSharper restore UnusedAutoPropertyAccessor.Global
        #pragma warning restore CS8618

        #endregion

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
            var tasks = _context.MAATasks.Where(t => t.ConnectionId == connection.Id && !t.IsCompleted)
                .Where(t=>t.CreatedAt>DateTime.UtcNow.AddMinutes(-5))
                .OrderByDescending(t => t.CreatedAt).Select(t => new {
                Id = t.Id.ToString("N"),
                Type = t.Type,
                Params = t.Parameters
            });

            // 检查任务列表是否为空
            if (!tasks.Any())
            {
                return Ok(new { tasks = Array.Empty<Object>() });
            }

            return Ok(new { tasks = tasks });
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
                    Payload = request.Payload,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.MAAResponses.Add(result);
                
                task.IsCompleted = true;
                task.CompletedAt = DateTime.UtcNow;

                _context.MAATasks.Update(task);

                await _context.SaveChangesAsync();

                _backgroundJobClient.Enqueue<MAAGenerateImageSnapshotService>(service => service.GenerateThumbnail(result.Id.ToString()));

                // 如果一切顺利，返回200 OK
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "在处理任务状态报告时出现异常。");
                return StatusCode(500, "服务器内部错误：" + ex.Message);
            }
        }
    }

}
