using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.Hangfire;
using AmiyaBotPlayerRatingServer.Model;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AmiyaBotPlayerRatingServer.Controllers.MAAControllers
{
    [ApiController]
    [Route("api/maaConnections")]
    public class MAARepetitiveTaskController : ControllerBase
    {
        private readonly PlayerRatingDatabaseContext _context;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly ILogger<MAARepetitiveTaskController> _logger;

        public MAARepetitiveTaskController(
            PlayerRatingDatabaseContext context,
            IBackgroundJobClient backgroundJobClient,
            IRecurringJobManager recurringJobManager,
            ILogger<MAARepetitiveTaskController> logger)
        {
            _context = context;
            _backgroundJobClient = backgroundJobClient;
            _recurringJobManager = recurringJobManager;
            _logger = logger;
        }

        #region Data Objects

#pragma warning disable CS8618
        // ReSharper disable UnusedAutoPropertyAccessor.Global

        public class AddRepetitiveTaskModel
        {
            public String Name { get; set; }
            public String Type { get; set; }
            public String Parameters { get; set; }
            public String UtcCronString { get; set; }
            public DateTime AvailableFrom { get; set; }
            public DateTime? AvailableTo { get; set; }
        }

        // ReSharper restore UnusedAutoPropertyAccessor.Global
#pragma warning restore CS8618

        #endregion


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpPost("{connectionId}/maaRepetitiveTasks")]
        public async Task<ActionResult<MAARepetitiveTask>> AddRepetitiveTask(Guid connectionId, [FromBody] AddRepetitiveTaskModel taskModel)
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
                    .FirstOrDefaultAsync(c => c.Id == connectionId && c.UserId == userId);

                if (connection == null)
                {
                    return NotFound("指定的连接不存在。");
                }

                var userTask = new MAARepetitiveTask
                {
                    Name = taskModel.Name,

                    ConnectionId = connection.Id,
                    Type = taskModel.Type,
                    Parameters = taskModel.Parameters,
                    UtcCronString = taskModel.UtcCronString,

                    CreatedAt = DateTime.UtcNow,

                    AvailableFrom = taskModel.AvailableFrom.ToUniversalTime(),
                    AvailableTo = taskModel.AvailableTo?.ToUniversalTime(),
                };

                await _context.MAARepetitiveTasks.AddAsync(userTask);
                await _context.SaveChangesAsync();

                _backgroundJobClient.Enqueue<MAAExecuteRepetitiveTaskService>(service => service.CreateTask(userTask.Id.ToString()));

                return Ok(new
                {
                    userTask.Id,
                    userTask.Name,
                    userTask.Type,
                    userTask.Parameters,
                    userTask.UtcCronString,
                    userTask.CreatedAt,
                    userTask.AvailableFrom,
                    userTask.AvailableTo,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建任务时发生错误。");
                return StatusCode(500, "创建任务时发生内部错误。");
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpDelete("{connectionId}/maaRepetitiveTasks/{taskId}")]
        public async Task<IActionResult> DeleteRepetitiveTask(Guid connectionId, Guid taskId)
        {
            // 获取当前用户ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("用户未登录。");
            }

            var userId = userIdClaim.Value;

            // 从数据库中找到对应的Connection
            var connection = await _context.MAAConnections
                .FirstOrDefaultAsync(c => c.Id == connectionId && c.UserId == userId);

            // 检查是否存在该Connection
            if (connection == null)
            {
                return NotFound("连接未找到。");
            }

            // 从数据库中找到对应的RepetitiveTask
            var repetitiveTask = await _context.MAARepetitiveTasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ConnectionId == connectionId);

            // 检查是否存在该RepetitiveTask
            if (repetitiveTask == null)
            {
                return NotFound("任务未找到。");
            }

            // 检查用户是否有权限删除该RepetitiveTask
            if (repetitiveTask.ConnectionId != connectionId)
            {
                return NotFound("任务未找到。");
            }

            // RepetitiveTask不可以真正的删除，只能标记为已删除，因为它的子任务可能还在运行并且用户查看任务历史时需要查看已删除的任务
            repetitiveTask.IsDeleted = true;
            var jobId = $"MAARepetitiveTask-{repetitiveTask.Id}";
            _recurringJobManager.RemoveIfExists(jobId);
            _context.MAARepetitiveTasks.Update(repetitiveTask);

            await _context.SaveChangesAsync();

            return Ok(new { Message = "成功删除任务。" });

        }
        
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpPost("{connectionId}/maaRepetitiveTasks/{taskId}/pause")]
        public async Task<IActionResult> PauseRepetitiveTask(Guid connectionId, Guid taskId)
        {
            // 获取当前用户ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("用户未登录。");
            }

            var userId = userIdClaim.Value;

            // 从数据库中找到对应的Connection
            var connection = await _context.MAAConnections
                .FirstOrDefaultAsync(c => c.Id == connectionId && c.UserId == userId);

            // 检查是否存在该Connection
            if (connection == null)
            {
                return NotFound("连接未找到。");
            }

            // 从数据库中找到对应的RepetitiveTask
            var repetitiveTask = await _context.MAARepetitiveTasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ConnectionId == connectionId);

            // 检查是否存在该RepetitiveTask
            if (repetitiveTask == null)
            {
                return NotFound("任务未找到。");
            }

            // 检查用户是否有权限删除该RepetitiveTask
            if (repetitiveTask.ConnectionId != connectionId)
            {
                return NotFound("任务未找到。");
            }

            // 暂停任务
            repetitiveTask.IsPaused = true;
            _context.MAARepetitiveTasks.Update(repetitiveTask);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "成功暂停任务。" });
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        // ReSharper disable once StringLiteralTypo
        [HttpPost("{connectionId}/maaRepetitiveTasks/{taskId}/unpause")]
        public async Task<IActionResult> UnPauseRepetitiveTask(Guid connectionId, Guid taskId)
        {
            // 获取当前用户ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("用户未登录。");
            }

            var userId = userIdClaim.Value;

            // 从数据库中找到对应的Connection
            var connection = await _context.MAAConnections
                .FirstOrDefaultAsync(c => c.Id == connectionId && c.UserId == userId);

            // 检查是否存在该Connection
            if (connection == null)
            {
                return NotFound("连接未找到。");
            }

            // 从数据库中找到对应的RepetitiveTask
            var repetitiveTask = await _context.MAARepetitiveTasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ConnectionId == connectionId);

            // 检查是否存在该RepetitiveTask
            if (repetitiveTask == null)
            {
                return NotFound("任务未找到。");
            }

            // 检查用户是否有权限删除该RepetitiveTask
            if (repetitiveTask.ConnectionId != connectionId)
            {
                return NotFound("任务未找到。");
            }

            // 恢复任务
            repetitiveTask.IsPaused = false;
            _context.MAARepetitiveTasks.Update(repetitiveTask);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "成功恢复任务。" });
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpGet("{connectionId}/maaRepetitiveTasks")]
        public async Task<ActionResult<IEnumerable<MAARepetitiveTask>>> ListRepetitiveTasks(Guid connectionId,
            [FromQuery] int? page, [FromQuery] int? size)
        {
            // 获取当前用户ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("用户未登录。");
            }

            var userId = userIdClaim.Value;

            // 从数据库中找到对应的Connection
            var connection = await _context.MAAConnections
                .FirstOrDefaultAsync(c => c.Id == connectionId && c.UserId == userId);

            // 检查是否存在该Connection
            if (connection == null)
            {
                return NotFound("连接未找到。");
            }

            IQueryable<MAARepetitiveTask> repetitiveTasks = _context.MAARepetitiveTasks
                .Where(t => t.ConnectionId == connectionId && t.IsDeleted == false)
                .OrderByDescending(t => t.CreatedAt);
            if (page != null && size != null)
            {
                repetitiveTasks = repetitiveTasks
                    .Skip(page.Value * size.Value)
                    .Take(size.Value);
            }

            var repetitiveTasksResult = await repetitiveTasks.Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Type,
                    t.Parameters,
                    t.UtcCronString,
                    t.CreatedAt,
                    t.AvailableFrom,
                    t.AvailableTo,
                })
                .ToListAsync();

            var total = await _context.MAARepetitiveTasks.CountAsync(t =>
                t.ConnectionId == connectionId && t.IsDeleted == false);

            return Ok(new
            {
                repetitiveTasks = repetitiveTasks,
                total = total,
                maxPage = repetitiveTasksResult.Count == 0 || size == null
                    ? 0
                    : Math.Ceiling((double)total / size.Value),
                page = page ?? 0,
                size = size ?? 0,
            });
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpGet("{connectionId}/maaRepetitiveTasks/{taskId}/image")]
        public async Task<IActionResult> GetTaskImage(Guid connectionId, Guid taskId, [FromQuery] String? type)
        {//type: original/null, thumbnail

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
                    .FirstOrDefaultAsync(c => c.Id == connectionId && c.UserId == userId);

                if (connection == null)
                {
                    return NotFound("指定的连接不存在。");
                }


                // 获取当前RepetitiveTask最新的对应的Task的对应截图
                var task = await _context.MAATasks
                    .Where(t => t.ParentRepetitiveTaskId == taskId && t.ConnectionId == connectionId)
                    .OrderByDescending(t => t.CreatedAt)
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

    }
}
