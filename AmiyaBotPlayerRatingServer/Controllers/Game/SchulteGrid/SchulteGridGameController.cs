using AmiyaBotPlayerRatingServer.GameLogic;
using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmiyaBotPlayerRatingServer.Controllers.Game.SchulteGrid
{
    [ApiController]
    [Route("api/schulteGridGame")]
    public class SchulteGridGameController : ControllerBase
    {

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpGet("{gameId}")]
        public IActionResult GetGame(String gameId)
        {
            var game = GameManager.GetGame(gameId) as SchulteGridGame;
            if (game == null)
            {
                return NotFound();
            }
            return Ok(
            new {
                game.Grid,
                game.PlayerList,
                game.Id,
                game.GameType,
            });
        }
    }
}
