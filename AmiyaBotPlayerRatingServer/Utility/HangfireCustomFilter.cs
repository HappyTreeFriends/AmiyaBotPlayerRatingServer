using Hangfire.Dashboard;
using System.Security.Claims;

namespace AmiyaBotPlayerRatingServer.Utility
{
    public class HangfireCustomFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            var roles = httpContext.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            return roles.Contains("管理员账户");
        }
    }
}
