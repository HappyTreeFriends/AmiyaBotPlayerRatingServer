using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.Model;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace AmiyaBotPlayerRatingServer.Controllers.Policy
{
    public static class CredentialOwnerPolicy
    {
        public const String Name = "CredentialOwner";

        public static void AddCredentialOwnerPolicy(this IServiceCollection service)
        {
            service.AddAuthorization(
                options =>
                {
                    options.AddPolicy(Name, policy =>
                        policy.Requirements.Add(new CredentialOwnerRequirement()));
                });

            service.AddScoped<IAuthorizationHandler, CredentialOwnerHandler>();

        }

        public class CredentialOwnerRequirement : IAuthorizationRequirement
        {

        }

        public class CredentialOwnerHandler : AuthorizationHandler<CredentialOwnerRequirement>
        {
            private readonly PlayerRatingDatabaseContext _context;
            private readonly IHttpContextAccessor _httpContextAccessor;

            public CredentialOwnerHandler(PlayerRatingDatabaseContext context, IHttpContextAccessor httpContextAccessor)
            {
                _context = context;
                _httpContextAccessor = httpContextAccessor;
            }

            protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CredentialOwnerRequirement requirement)
            {
                // 从context获取当前用户ID
                var currentUserId = context.User.FindFirst(ClaimTypes.Name)?.Value;

                if (string.IsNullOrEmpty(currentUserId))
                {
                    context.Fail();
                    return;
                }

                // 从HttpContext获取目标Credential ID
                var targetCredentialId = _httpContextAccessor.HttpContext?.Request.RouteValues["credentialId"] as string;

                if (string.IsNullOrEmpty(targetCredentialId))
                {
                    context.Fail();
                    return;
                }

                // 查询数据库以确认currentUserId是targetCredentialId的拥有者
                var targetCredential = await _context.Set<SKLandCredential>()
                    .Where(c => c.Id == targetCredentialId && c.UserId == currentUserId)
                    .FirstOrDefaultAsync();

                if (targetCredential != null)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
            }
        }
    }

}
