using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Validation.AspNetCore;
using static AmiyaBotPlayerRatingServer.Controllers.Policy.OpenIddictScopePolicy;

namespace AmiyaBotPlayerRatingServer.Controllers.Policy
{
    public static class SystemRolePolicy
    {
        public const string AdminRole = "管理员账户";
        public const string DeveloperRole = "开发者账户";
        public const string UserRole = "普通账户";
        public const string DemoUserRole = "演示普通账户";
        public const string DemoDeveloperRole = "演示开发者账户";

        public static async void AddSystemRoleAsync(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                if (!await roleManager.RoleExistsAsync(AdminRole))
                {
                    await roleManager.CreateAsync(new IdentityRole(AdminRole));
                }

                if (!await roleManager.RoleExistsAsync(DeveloperRole))
                {
                    await roleManager.CreateAsync(new IdentityRole(DeveloperRole));
                }

                if (!await roleManager.RoleExistsAsync(UserRole))
                {
                    await roleManager.CreateAsync(new IdentityRole(UserRole));
                }

                if (!await roleManager.RoleExistsAsync(DemoUserRole))
                {
                    await roleManager.CreateAsync(new IdentityRole(DemoUserRole));
                }

                if (!await roleManager.RoleExistsAsync(DemoDeveloperRole))
                {
                    await roleManager.CreateAsync(new IdentityRole(DemoDeveloperRole));
                }
            }
        }
    }

}
