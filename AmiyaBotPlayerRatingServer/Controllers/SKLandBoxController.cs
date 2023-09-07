using Microsoft.AspNetCore.Mvc;

namespace AmiyaBotPlayerRatingServer.Controllers
{
    public class SKLandBoxController : ControllerBase
    {
        public IActionResult GetBoxByCredential(string credentialId)
        {
            return Ok();
        }
    }

}
