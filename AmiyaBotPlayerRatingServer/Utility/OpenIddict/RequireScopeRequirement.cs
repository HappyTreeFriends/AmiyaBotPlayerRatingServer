using Microsoft.AspNetCore.Authorization;

namespace AmiyaBotPlayerRatingServer.Utility.OpenIddict
{
    using Microsoft.AspNetCore.Authorization;

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
