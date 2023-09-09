using Hangfire.Dashboard;

namespace AmiyaBotPlayerRatingServer.Utility
{
    public class HangfireCustomFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            return httpContext.User.Identity?.IsAuthenticated ?? false;
        }
    }
}
