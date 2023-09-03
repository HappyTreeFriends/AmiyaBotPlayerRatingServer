using AmiyaBotPlayerRatingServer.Data;
using Hangfire;
using Hangfire.PostgreSql;

namespace AmiyaBotPlayerRatingServer.Utility
{
    public class HangfireConfigurationService
    {
        public HangfireConfigurationService(IConfiguration configuration)
        {
            GlobalConfiguration.Configuration.UsePostgreSqlStorage(PlayerRatingDatabaseContext.GetConnectionString(configuration));
        }
    }
}
