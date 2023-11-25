using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.Model;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static AmiyaBotPlayerRatingServer.Controllers.MAAControllers.MAAConnectionController;
using System.Security.Claims;

namespace AmiyaBotPlayerRatingServer.Controllers.MAAControllers
{
    [ApiController]
    [Route("api/maaConnections")]
    public class MAATaskController : ControllerBase
    {
        private readonly PlayerRatingDatabaseContext _context;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly ILogger<MAATaskController> _logger;

        public MAATaskController(
            PlayerRatingDatabaseContext context,
            IBackgroundJobClient backgroundJobClient,
            IRecurringJobManager recurringJobManager,
            ILogger<MAATaskController> logger)
        {
            _context = context;
            _backgroundJobClient = backgroundJobClient;
            _recurringJobManager = recurringJobManager;
            _logger = logger;
        }

        #region Data Objects

#pragma warning disable CS8618
        // ReSharper disable UnusedAutoPropertyAccessor.Global

        public class AddTaskModel
        {
            public String Type { get; set; }
            public String Parameters { get; set; }
        }
        // ReSharper restore UnusedAutoPropertyAccessor.Global
#pragma warning restore CS8618

        #endregion

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpGet("{id}/maaTasks")]
        public async Task<IActionResult> ListTasks(Guid id, [FromQuery] string? repetitiveTaskId, [FromQuery] int page, [FromQuery] int size, [FromQuery] bool showSystem = false)
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
                var connection = await _context.MAAConnections
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

                if (connection == null)
                {
                    return NotFound("指定的连接不存在。");
                }

                var tasks = await _context.MAATasks
                    .Where(t => t.ConnectionId == connection.Id && (showSystem || !t.IsSystemGenerated))
                    .OrderByDescending(t => t.CreatedAt)
                    .Where(t => repetitiveTaskId == null || t.ParentRepetitiveTaskId == Guid.Parse(repetitiveTaskId))
                    .Skip(page * size)
                    .Take(size)
                    .Select(t => new
                    {
                        t.Id,
                        t.Type,
                        t.Parameters,
                        t.IsCompleted,
                        t.CreatedAt,
                        t.CompletedAt,
                        t.IsSystemGenerated,
                        t.ParentRepetitiveTaskId,
                    })
                    .ToListAsync();

                var total = await _context.MAATasks.Where(t => repetitiveTaskId == null || t.ParentRepetitiveTaskId == Guid.Parse(repetitiveTaskId)).CountAsync(t => t.ConnectionId == connection.Id);

                return Ok(new
                {
                    tasks = tasks,
                    total = total,
                    maxPage = tasks.Count == 0 ? 0 : Math.Ceiling((double)total / size),
                    page = page,
                    size = size,

                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "列出任务时发生错误。");
                return StatusCode(500, "获取任务列表时发生内部错误。");
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpGet("{id}/maaTasks/{taskId}")]
        public async Task<IActionResult> GetTask(Guid id, Guid taskId)
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
                var connection = await _context.MAAConnections
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

                if (connection == null)
                {
                    return NotFound("指定的连接不存在。");
                }

                var task = await _context.MAATasks
                    .Where(t => t.ConnectionId == connection.Id && t.Id == taskId)
                    .Select(t => new
                    {
                        t.Id,
                        t.Type,
                        t.Parameters,
                        t.IsCompleted,
                        t.CreatedAt,
                        t.CompletedAt
                    })
                    .FirstOrDefaultAsync();

                if (task == null)
                {
                    return NotFound("指定的任务不存在。");
                }

                return Ok(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取任务时发生错误。");
                return StatusCode(500, "获取任务时发生内部错误。");
            }
        }
        
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpGet("{id}/maaTasks/{taskId}/image")]
        public async Task<IActionResult> GetTaskImage(Guid id, Guid taskId, [FromQuery] String? type)
        {
            //type: original/null, thumbnail

            // 从JWT中提取用户ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("用户未登录。");
            }

            var userId = userIdClaim.Value;

            try
            {
                var connection = await _context.MAAConnections
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

                if (connection == null)
                {
                    return NotFound("指定的连接不存在。");
                }

                var task = await _context.MAATasks
                    .Where(t => t.ConnectionId == connection.Id && t.Id == taskId)
                    .Include(t => t.SubTasks)
                    .FirstOrDefaultAsync();

                if (task == null)
                {
                    return NotFound("指定的任务不存在。");
                }

                MAAResponse? result = null;
                if (task.Type == "CaptureImage" || task.Type == "CaptureImageNow")
                {
                    result = _context.MAAResponses.FirstOrDefault(r => r.TaskId == task.Id);
                }
                else
                {
                    if (task.SubTasks?.Count > 0)
                    {
                        var capSubTask = task.SubTasks.FirstOrDefault(t => t.Type == "CaptureImage")?.Id;
                        result = _context.MAAResponses.FirstOrDefault(r => r.TaskId == capSubTask);
                    }
                }

                if (result == null)
                {
                    return Ok(new
                    {
                        Image = ""
                    });
                }

                if (type == "thumbnail")
                {
                    return Ok(new
                    {
                        Image = result.ImagePayloadThumbnail
                    });
                }
                else
                {
                    return Ok(new
                    {
                        Image = result.ImagePayload
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取任务时发生错误。");
                return StatusCode(500, "获取任务时发生内部错误。");
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpPost("{id}/maaTasks")]
        public async Task<IActionResult> AddTask(Guid id, [FromBody] AddTaskModel taskModel)
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
                var connection = await _context.MAAConnections
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

                if (connection == null)
                {
                    return NotFound("指定的连接不存在。");
                }

                var userTask = new MAATask
                {
                    ConnectionId = connection.Id,
                    Type = taskModel.Type,
                    Parameters = taskModel.Parameters,
                    CreatedAt = DateTime.UtcNow,
                    IsCompleted = false,
                    IsSystemGenerated = false
                };

                MAATask? captureTask = null;

                if (userTask.Type != "CaptureImage" && userTask.Type != "CaptureImageNow")
                {
                    captureTask = new MAATask
                    {
                        ConnectionId = connection.Id,
                        Type = "CaptureImage",
                        Parameters = null,
                        CreatedAt = DateTime.UtcNow,
                        IsCompleted = false,
                        IsSystemGenerated = true,
                        ParentTask = userTask
                    };
                }

                await _context.MAATasks.AddAsync(userTask);
                if (captureTask != null)
                {
                    await _context.MAATasks.AddAsync(captureTask);
                }
                await _context.SaveChangesAsync();


                return Ok(new
                {
                    userTask.Id,
                    userTask.Type,
                    userTask.Parameters,
                    userTask.IsCompleted,
                    userTask.CreatedAt,
                    userTask.CompletedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建任务时发生错误。");
                return StatusCode(500, "创建任务时发生内部错误。");
            }
        }

    }
}
