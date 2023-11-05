using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.Model;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace AmiyaBotPlayerRatingServer.Hangfire
{
    public class MAATakeSnapshotOnAllConnectionsService
    {
        private readonly PlayerRatingDatabaseContext _dbContext;
        private readonly ILogger<MAATakeSnapshotOnAllConnectionsService> _logger;

        public MAATakeSnapshotOnAllConnectionsService(PlayerRatingDatabaseContext dbContext,ILogger<MAATakeSnapshotOnAllConnectionsService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Collect()
        {
            var tasksToAdd = new List<MAATask>();
            foreach (var connection in _dbContext.MAAConnections)
            {
                var task = new MAATask
                {
                    Id = Guid.NewGuid(),
                    ConnectionId = connection.Id,
                    Type = "CaptureImage",
                    Parameters = null,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                tasksToAdd.Add(task);
            }

            _dbContext.MAATasks.AddRange(tasksToAdd);
            await _dbContext.SaveChangesAsync();

            _logger.Log(LogLevel.Information,$"已创建{tasksToAdd.Count}个截图任务。");
        }

    }
}
