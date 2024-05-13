using AmiyaBotPlayerRatingServer.Data;
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
        private readonly PlayerRatingDatabaseContext _dbContext;

        public SchulteGridGameController(PlayerRatingDatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        private object GetGameReturnObj(SchulteGridGame game)
        {
            var creator = _dbContext.Users.Find(game.CreatorId);

            return new
            {
                game.Grid,
                game.PlayerList,
                game.Id,
                game.GameType,
                game.CreatorId,
                game.IsStarted,
                game.IsCompleted,
                game.IsPrivate,
                game.CreatorConnectionId,
                CreatorNickname = creator?.Nickname,
            };
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpGet("{gameId}")]
        public IActionResult GetGame(String gameId)
        {
            var game = GameManager.GetGame(gameId) as SchulteGridGame;
            if (game == null)
            {
                return NotFound();
            }

            return Ok(GetGameReturnObj(game));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpGet]
        public IActionResult ListGame()
        {
            var list = GameManager.GameList.OfType<SchulteGridGame>().Where(g => g.IsPrivate == false && g.IsCompleted == false).Select(GetGameReturnObj);
            return Ok(list);
        }
    }
}
