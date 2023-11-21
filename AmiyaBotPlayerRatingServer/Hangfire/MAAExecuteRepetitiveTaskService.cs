using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.Model;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Storage;

namespace AmiyaBotPlayerRatingServer.Hangfire
{
    public class MAAExecuteRepetitiveTaskService
    {
        private readonly PlayerRatingDatabaseContext _dbContext;
        private readonly ILogger<MAAExecuteRepetitiveTaskService> _logger;
        private readonly IRecurringJobManager _jobManager;
        private readonly IMonitoringApi _monitoringApi;

        public MAAExecuteRepetitiveTaskService(PlayerRatingDatabaseContext dbContext,ILogger<MAAExecuteRepetitiveTaskService> logger,
            IRecurringJobManager jobManager,IMonitoringApi monitoringApi)
        {
            _dbContext = dbContext;
            _logger = logger;
            _jobManager = jobManager;
            _monitoringApi = monitoringApi;
        }

        public void CreateTask(String repetitiveTaskId)
        {
            //从数据库中直接创建它的Hangfire任务，根据CronStr
            var repetitiveTask = _dbContext.MAARepetitiveTasks.FirstOrDefault(r => r.Id == Guid.Parse(repetitiveTaskId));
            if (repetitiveTask == null||repetitiveTask.IsDeleted)
            {
                return;
            }

            //组织任务Id
            var jobId = $"MAARepetitiveTask-{repetitiveTask.Id}";
            
            var cronStr = repetitiveTask.UtcCronString;
            if (String.IsNullOrWhiteSpace(cronStr))
            {
                return;
            }

            _jobManager.AddOrUpdate<MAAExecuteRepetitiveTaskService>(jobId, service => service.ExecuteRepetitiveTask(repetitiveTaskId,false), cronStr);
            
        }

        public async Task ExecuteRepetitiveTask(String repetitiveTaskId,bool  lastTask = true)
        {
            //实际根据该重复任务创建真正的任务
            var repetitiveTask = _dbContext.MAARepetitiveTasks.FirstOrDefault(r => r.Id == Guid.Parse(repetitiveTaskId));

            if (repetitiveTask == null || repetitiveTask.IsDeleted)
            {
                return;
            }

            //判断AvailableFrom和AvailableTo
            var now = DateTime.UtcNow;
            if (repetitiveTask.AvailableFrom > now)
            {
                return;
            }

            if (lastTask == false && repetitiveTask.AvailableTo.HasValue && repetitiveTask.AvailableTo.Value < now)
            {
                return;
            }

            //创建任务
            MAATask? captureTask = null;

            var userTask = new MAATask
            {
                ConnectionId = repetitiveTask.ConnectionId,
                IsCompleted = false,
                IsSystemGenerated = true,
                Type = repetitiveTask.Type,
                Parameters = repetitiveTask.Parameters,
                AvailableAt = DateTime.UtcNow,
                ParentRepetitiveTaskId = repetitiveTask.Id
            };

            //额外的CaptureImage
            if (userTask.Type != "CaptureImage" && userTask.Type != "CaptureImageNow")
            {
                captureTask = new MAATask
                {
                    ConnectionId = repetitiveTask.ConnectionId,
                    Type = "CaptureImage",
                    Parameters = null,
                    CreatedAt = DateTime.UtcNow,
                    IsCompleted = false,
                    IsSystemGenerated = true,
                    ParentTask = userTask
                };
            }

            await _dbContext.MAATasks.AddAsync(userTask);
            if (captureTask != null)
            {
                await _dbContext.MAATasks.AddAsync(captureTask);
            }
            await _dbContext.SaveChangesAsync();
        }

    }
}
