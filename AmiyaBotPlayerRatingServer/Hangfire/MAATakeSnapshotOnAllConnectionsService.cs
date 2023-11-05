using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.Model;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace AmiyaBotPlayerRatingServer.Hangfire
{
    public class MAATakeSnapshotOnAllConnectionsService
    {
        private readonly IConfiguration _configuration;
        private readonly PlayerRatingDatabaseContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;

        public MAATakeSnapshotOnAllConnectionsService(IConfiguration configuration, PlayerRatingDatabaseContext dbContext, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
        }

        public async Task Collect()
        {
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

                _dbContext.MAATasks.Add(task);
                await _dbContext.SaveChangesAsync();
            }
        }

    }
}
