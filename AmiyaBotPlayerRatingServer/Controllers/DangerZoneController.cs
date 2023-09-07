using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using System.Data;

namespace AmiyaBotPlayerRatingServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DangerZoneController : ControllerBase
    {
        private readonly IOpenIddictScopeManager _scopeManager;

        public DangerZoneController(IOpenIddictScopeManager scopeManager)
        {
            _scopeManager = scopeManager;
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Authorize(Roles = "管理员账户")]
        [HttpPost("reset-scope")]
        public async Task<object> ResetScope()
        {
            await AddScope("TestWriteData","写入数据");
            await AddScope("TestReadData","读取数据");

            return Ok();
        }

        private async Task AddScope(string scopeName, string scopeDisplayName)
        {
            // 检查是否已经存在该 scope
            var existingScope = await _scopeManager.FindByNameAsync(scopeName);

            if (existingScope == null)
            {
                // 如果不存在，则创建新的 scope
                var descriptor = new OpenIddictScopeDescriptor
                {
                    Name = scopeName,
                    DisplayName = scopeDisplayName
                };

                await _scopeManager.CreateAsync(descriptor);
            }
        }
    }
}