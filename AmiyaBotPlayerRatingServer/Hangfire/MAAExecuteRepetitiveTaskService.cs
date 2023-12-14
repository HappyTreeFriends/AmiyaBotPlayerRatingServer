using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.Model;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using AmiyaBotPlayerRatingServer.Services.MAAServices;
using Hangfire;
using Hangfire.Storage;

namespace AmiyaBotPlayerRatingServer.Hangfire
{
    public class MAAExecuteRepetitiveTaskService
    {
        private readonly PlayerRatingDatabaseContext _dbContext;
        private readonly ILogger<MAAExecuteRepetitiveTaskService> _logger;
        private readonly IRecurringJobManager _jobManager;
        private readonly CreateMAATaskService _createMAATaskService;

        public MAAExecuteRepetitiveTaskService(PlayerRatingDatabaseContext dbContext,ILogger<MAAExecuteRepetitiveTaskService> logger,
            IRecurringJobManager jobManager,CreateMAATaskService createMAATaskService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _jobManager = jobManager;
            _createMAATaskService = createMAATaskService;
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

            if (repetitiveTask.IsPaused)
            {
                return;
            }
            
            await _createMAATaskService.CreateMAATask(repetitiveTask.ConnectionId, repetitiveTask.Type, repetitiveTask.Parameters,repetitiveTask.Id);
        }

    }
}
