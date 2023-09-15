using Microsoft.AspNetCore.Authorization;
using OpenIddict.Server.AspNetCore;

namespace AmiyaBotPlayerRatingServer.Controllers.Policy
{
    public static class OpenIddictScopePolicy
    {
        public const String TestWriteDataPolicy = "TestWriteData";

        public static void AddOpenIddictScopePolicy(this IServiceCollection service)
        {
            service.AddAuthorization(
                options =>
                {
                    options.AddPolicy(TestWriteDataPolicy, policy =>
                    {
                        policy.AuthenticationSchemes.Add(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                        policy.Requirements.Add(new RequireScopeRequirement(TestWriteDataPolicy));
                    });
                });

            service.AddSingleton<IAuthorizationHandler, RequireScopeHandler>();

        }

        public class RequireScopeHandler : AuthorizationHandler<RequireScopeRequirement>
        {
            protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RequireScopeRequirement requirement)
            {
                // 在此处检查用户是否具有所需的范围
                if (context.User.HasClaim("scope", requirement.Scope))
                {
                    context.Succeed(requirement);
                }

                return Task.CompletedTask;
            }
        }
        
        public class RequireScopeRequirement : IAuthorizationRequirement
        {
            public string Scope { get; }

            public RequireScopeRequirement(string scope)
            {
                Scope = scope ?? throw new ArgumentNullException(nameof(scope));
            }
        }
    }
}
