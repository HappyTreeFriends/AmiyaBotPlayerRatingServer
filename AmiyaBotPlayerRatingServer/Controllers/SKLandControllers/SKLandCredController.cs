using AmiyaBotPlayerRatingServer.Controllers.Policy;
using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.Hangfire;
using AmiyaBotPlayerRatingServer.Model;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly;
using System.Security.Claims;

namespace AmiyaBotPlayerRatingServer.Controllers.SKLandControllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SKLandCredentialController(
        PlayerRatingDatabaseContext context,
        IBackgroundJobClient backgroundJobClient)
        : ControllerBase
    {
#pragma warning disable CS8618
        // ReSharper disable UnusedAutoPropertyAccessor.Global
        public class SKLandCredentialModel
        {
            public string Credential { get; set; }
            // 可能还有其他字段，比如昵称、头像URL等
        }
        // ReSharper restore UnusedAutoPropertyAccessor.Global
#pragma warning restore CS8618

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpPost("Create")]
        public async Task<IActionResult> CreateCredential([FromBody] SKLandCredentialModel model)
        {
            // 从当前用户的Claims获取用户ID
            var userId = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // 验证Credential是否已经存在
            var existingCredential = await context.SKLandCredentials
                .FirstOrDefaultAsync(c => c.Credential == model.Credential && c.UserId == userId);

            if (existingCredential != null)
            {
                return BadRequest("Credential already exists for this user.");
            }

            // 创建新的SKLandCredential
            var newCredential = new SKLandCredential
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Credential = model.Credential,
                SKLandUid = "",
                Nickname = "",
                AvatarUrl = ""
            };

            context.SKLandCredentials.Add(newCredential);

            // 保存更改            
            await context.SaveChangesAsync();

            backgroundJobClient.Enqueue<CollectPlayerInformationService>(service => service.Collect(newCredential.Id));

            return Ok(new { newCredential.Id, Message = "Credential successfully created." });
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpPut("Update/{credentialId}")]
        [Authorize(Policy = CredentialOwnerPolicy.Name)]
        public async Task<IActionResult> UpdateCredential(string credentialId, [FromBody] SKLandCredentialModel model)
        {
            // 获取当前用户ID
            var userId = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // 从数据库中找到对应的Credential
            var credentialToUpdate = await context.SKLandCredentials.FindAsync(new Guid(credentialId));

            if (credentialToUpdate == null)
            {
                return NotFound("Credential not found.");
            }

            // 更新字段
            credentialToUpdate.Credential = model.Credential;
            // 如果有其他字段（比如昵称、头像等），也应在这里进行更新

            await context.SaveChangesAsync();

            return Ok(new { Message = "Credential successfully updated." });
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpDelete("Delete/{credentialId}")]
        [Authorize(Policy = CredentialOwnerPolicy.Name)]
        public async Task<IActionResult> DeleteCredential(string credentialId)
        {
            // 获取当前用户ID
            var userId = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            if (!Guid.TryParse(credentialId, out _))
            {
                return BadRequest("Invalid credential ID format.");
            }

            // 从数据库中找到对应的Credential
            var credentialToDelete = await context.SKLandCredentials
                .Where(c => c.Id == credentialId && c.UserId == userId)
                .FirstOrDefaultAsync();

            if (credentialToDelete == null)
            {
                return NotFound("Credential not found.");
            }

            // 删除该Credential
            context.SKLandCredentials.Remove(credentialToDelete);

            await context.SaveChangesAsync();

            return Ok(new { Message = "Credential successfully deleted." });
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户,演示普通账户")]
        [HttpGet("List")]
        public async Task<IActionResult> GetCredentials()
        {
            // 获取当前用户ID
            var userId = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // 从数据库获取该用户的所有Credentials\
            try
            {
                var credentials = await context.SKLandCredentials
                    .Where(c => c.UserId == userId)
                    .Select(c => new
                    {
                        c.Id,
                        c.Credential,
                        c.Nickname,
                        c.AvatarUrl,
                        c.RefreshedAt,
                        c.RefreshSuccess,
                    })
                    .ToListAsync();


                return Ok(credentials);
            }
            catch (Exception)
            {
                // 记录异常或执行其他错误处理逻辑
                return StatusCode(500, "An error occurred while retrieving the credentials.");
            }

        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户,演示普通账户")]
        [HttpGet("Details/{credentialId}")]
        [Authorize(Policy = CredentialOwnerPolicy.Name)]
        public async Task<IActionResult> GetCredentialDetails(string credentialId)
        {
            // 获取当前用户ID
            var userId = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // 从数据库中找到对应的Credential
            var credentialDetails = await context.SKLandCredentials
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == credentialId);

            if (credentialDetails == null)
            {
                return NotFound("Credential not found.");
            }

            return Ok(new
            {
                credentialDetails.Id,
                credentialDetails.Credential,
                credentialDetails.Nickname,
                Avatar = credentialDetails.AvatarUrl,
            });
        }
    }

}
