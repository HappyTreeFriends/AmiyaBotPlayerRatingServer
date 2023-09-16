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

namespace AmiyaBotPlayerRatingServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,Roles= "普通账户")]
    public class SKLandCredentialController : ControllerBase
    {
        private readonly PlayerRatingDatabaseContext _context;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public SKLandCredentialController(PlayerRatingDatabaseContext context, IBackgroundJobClient backgroundJobClient)
        {
            _context = context;
            _backgroundJobClient= backgroundJobClient;
        }

        public class SKLandCredentialModel
        {
            public string Credential { get; set; }
            // 可能还有其他字段，比如昵称、头像URL等
        }

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
            var existingCredential = await _context.SKLandCredentials
                .FirstOrDefaultAsync(c => c.Credential == model.Credential && c.UserId == userId);

            if (existingCredential != null)
            {
                return BadRequest("Credential already exists for this user.");
            }

            // 创建新的SKLandCredential
            var newCredential = new SKLandCredential
            {
                Id= Guid.NewGuid().ToString(),
                UserId = userId,
                Credential = model.Credential,
                SKLandUid = "",
                Nickname = "",
                AvatarUrl = ""
            };

            _context.SKLandCredentials.Add(newCredential);

            // 保存更改            
            await _context.SaveChangesAsync();

            _backgroundJobClient.Enqueue<CollectPlayerInformationService>(service => service.Collect(newCredential.Id));

            return Ok(new { Id = newCredential.Id, Message = "Credential successfully created." });
        }

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
            var credentialToUpdate = await _context.SKLandCredentials.FindAsync(new Guid(credentialId));

            if (credentialToUpdate == null)
            {
                return NotFound("Credential not found.");
            }

            // 更新字段
            credentialToUpdate.Credential = model.Credential;
            // 如果有其他字段（比如昵称、头像等），也应在这里进行更新
            
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Credential successfully updated." });
        }

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

            if (!Guid.TryParse(credentialId, out var guidCredentialId))
            {
                return BadRequest("Invalid credential ID format.");
            }

            // 从数据库中找到对应的Credential
            var credentialToDelete = await _context.SKLandCredentials
                .Where(c => c.Id == credentialId && c.UserId == userId)
                .FirstOrDefaultAsync();

            if (credentialToDelete == null)
            {
                return NotFound("Credential not found.");
            }

            // 删除该Credential
            _context.SKLandCredentials.Remove(credentialToDelete);
            
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Credential successfully deleted." });
        }

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
                var credentials = await _context.SKLandCredentials
                    .Where(c => c.UserId == userId)
                    .Select(c => new
                    {
                        Id = c.Id,
                        Credential = c.Credential,
                        c.Nickname,
                        c.AvatarUrl
                    })
                    .ToListAsync();


                return Ok(credentials);
            }
            catch (Exception ex)
            {
                // 记录异常或执行其他错误处理逻辑
                return StatusCode(500, "An error occurred while retrieving the credentials.");
            }

        }

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
            var credentialDetails = await _context.SKLandCredentials
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == credentialId);

            if (credentialDetails == null)
            {
                return NotFound("Credential not found.");
            }

            return Ok(new
            {
                Id = credentialDetails.Id,
                Credential = credentialDetails.Credential,
                Nickname = credentialDetails.Nickname,
                Avatar = credentialDetails.AvatarUrl,
            });
        }
    }

}
