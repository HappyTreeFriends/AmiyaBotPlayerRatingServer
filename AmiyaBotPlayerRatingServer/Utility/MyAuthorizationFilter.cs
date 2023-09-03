using Hangfire.Dashboard;

namespace AmiyaBotPlayerRatingServer.Utility
{
    public class MyAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            
            return true;
        }
    }
}
