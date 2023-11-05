using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AmiyaBotPlayerRatingServer.Controllers.Policy;
using AmiyaBotPlayerRatingServer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using OpenIddict.Validation.AspNetCore;
using static AmiyaBotPlayerRatingServer.Controllers.SKLandControllers.SKLandCredentialController;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace AmiyaBotPlayerRatingServer.Controllers.SKLandControllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SKLandBoxController : ControllerBase
    {
        private readonly PlayerRatingDatabaseContext _context;

        public SKLandBoxController(PlayerRatingDatabaseContext context)
        {
            _context = context;
        }

        [HttpGet("GetBoxByCredential/{credentialId}")]
        [Authorize(Policy = CredentialOwnerPolicy.Name)]
        public async Task<IActionResult> GetBoxByCredential(string credentialId)
        {
            // 获取当前用户ID
            var userId = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // 从数据库中找到对应的CharacterBox
            var characterBox = await _context.SKLandCharacterBoxes
                .Include(box => box.Credential)  // Include the related SKLandCredential
                .AsNoTracking()
                .FirstOrDefaultAsync(box => box.CredentialId == credentialId);

            if (characterBox == null)
            {
                return NotFound("Character box not found.");
            }

            return Ok(new
            {
                characterBox.Id,
                characterBox.CredentialId,
                characterBox.CharacterBoxJson
            });
        }

        public class SKLandGetBoxModel
        {
            public string PartList { get; set; }
        }

        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        [HttpPost("GetBox")]
        public async Task<IActionResult> GetBox([FromBody] SKLandGetBoxModel model)
        {
            // 获取当前用户ID
            var userId = User.FindFirst(Claims.Subject)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var credClaimValue = User.FindFirst("SKLandCredentialId")?.Value;

            if (string.IsNullOrEmpty(credClaimValue))
            {
                return NotFound("该凭据没有对应的森空岛凭据.");
            }

            // 从数据库中找到对应的CharacterBox
            var characterBox = await _context.SKLandCharacterBoxes
                .Where(b => b.CredentialId == credClaimValue).OrderByDescending(b => b.RefreshedAt)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (characterBox == null)
            {
                return NotFound("Character box not found.");
            }

            var infoData = JObject.Parse(characterBox.CharacterBoxJson);

            var boxParts = model.PartList.Split(',');

            var retObject = new Dictionary<string, object>();

            foreach (var boxPart in boxParts)
            {
                if (boxPart == "status")
                {
                    //不允许直接访问Status块
                    var tempStatusBlock = new Dictionary<string, object>();
                    var statusData = infoData?["status"];

                    //加密Name
                    string pattern = @"^(.*)#(\d*)$";
                    var playerName = statusData["name"]?.ToString() ?? "海猫络合物#0000";
                    string result = Regex.Replace(playerName, pattern, m =>
                    {
                        string preHash = m.Groups[1].Value;
                        string postHash = m.Groups[2].Value;

                        preHash = preHash.Length <= 2 ? preHash : preHash[0] + new string('*', preHash.Length - 2) + preHash[^1];
                        postHash = postHash.Length <= 2 ? postHash : postHash[0] + new string('*', postHash.Length - 2) + postHash[^1];

                        return preHash + "#" + postHash;
                    });
                    tempStatusBlock.Add("name", result);
                    tempStatusBlock.Add("nameEncrypted", true);

                    tempStatusBlock.Add("level", statusData["level"]);
                    tempStatusBlock.Add("avatar", statusData["avatar"]);
                    tempStatusBlock.Add("mainStageProgress", statusData["mainStageProgress"]);
                    tempStatusBlock.Add("secretary", statusData["secretary"]);

                    retObject["status"] = tempStatusBlock;
                }
                else
                {
                    if (infoData.ContainsKey(boxPart))
                    {
                        retObject[boxPart] = infoData[boxPart];
                    }
                }
            }

            var retObj = new
            {
                characterBox.Id,
                characterBox.CredentialId,
                code = 0,
                message = "OK",
                data = retObject
            };

            return new ContentResult
            {
                Content = JsonConvert.SerializeObject(retObj),
                ContentType = "application/json",
                StatusCode = 200
            };
        }
    }
}