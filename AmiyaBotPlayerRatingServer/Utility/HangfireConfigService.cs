using AmiyaBotPlayerRatingServer.Data;
using Hangfire;
using Hangfire.PostgreSql;

namespace AmiyaBotPlayerRatingServer.Utility
{
    public class HangfireConfigService
    {
        public HangfireConfigService(IConfiguration configuration)
        {
            GlobalConfiguration.Configuration.UsePostgreSqlStorage(PlayerRatingDatabaseContext.GetConnectionString(configuration));
        }
    }
}
