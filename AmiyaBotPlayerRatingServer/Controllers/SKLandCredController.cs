using AmiyaBotPlayerRatingServer.Controllers.Policy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Polly;

namespace AmiyaBotPlayerRatingServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SKLandCredentialController : ControllerBase
    {
        [HttpPost("Create")]
        public IActionResult CreateCredential()
        {
            return Ok();
        }

        [HttpPut("Update/{credentialId}")]
        [Authorize(Policy = CredentialOwnerPolicy.Name)]
        public IActionResult UpdateCredential(string credentialId)
        {
            return Ok();
        }

        [HttpDelete("Delete/{credentialId}")]
        [Authorize(Policy = CredentialOwnerPolicy.Name)]
        public IActionResult DeleteCredential(string credentialId)
        {
            return Ok();
        }

        [HttpGet("List")]
        public IActionResult GetCredentials()
        {
            return Ok();
        }

        [HttpGet("Detail/{credentialId}")]
        [Authorize(Policy = CredentialOwnerPolicy.Name)]
        public IActionResult GetCredentialDetails(string credentialId)
        {
            return Ok();
        }
    }

}
