using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.GameLogic;
using AmiyaBotPlayerRatingServer.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmiyaBotPlayerRatingServer.Controllers
{
    [ApiController]
    [Route("api/server")]
    public class ServerController(
        PlayerRatingDatabaseContext dbContext,
        IConfiguration configuration,
        ArknightsMemoryCache memoryCache)
        : ControllerBase
    {
#pragma warning disable CS8618
        // ReSharper disable UnusedAutoPropertyAccessor.Global
        public class SendNotificationModel
        {
            public string Message { get; set; }
            public DateTime ExpiredAt { get; set; }
        }

        public class RefreshArknightsDataModel
        {
            public string Commit { get; set; }
        }
        // ReSharper restore UnusedAutoPropertyAccessor.Global
#pragma warning restore CS8618

        [AllowAnonymous]
        [HttpGet("serverStatistics")]
        public async Task<IActionResult> GetServerStatistics()
        {
            var totalPlaying = await dbContext.GameInfos.Where(c => c.IsClosed != true).CountAsync();
            var totalGames = await dbContext.GameInfos.CountAsync();
            var validPlayers = await dbContext.ApplicationUserMinigameStatistics.CountAsync();

            return Ok(new
            {
                GamesPlaying = totalPlaying,
                GamesTotal = totalGames,
                PlayersTotal = validPlayers
            });
        }

        [Authorize(Roles = "管理员账户")]
        [HttpPost("sendNotificationToAll")]
        public async Task<IActionResult> SendNotificationToAll([FromBody] SendNotificationModel model)
        {
            var not = new SystemNotification
            {
                Id = Guid.NewGuid(),
                Message = model.Message,
                ExpiredAt = model.ExpiredAt
            };

            dbContext.SystemNotifications.Add(not);
            await dbContext.SaveChangesAsync();

            return Ok();
        }

        [Authorize(Roles = "管理员账户")]
        [HttpPost("refreshArknightsData")]
        public async Task<IActionResult> RefreshArknightsData([FromBody] RefreshArknightsDataModel model)
        {

            try
            {
                memoryCache.UpdateAssets(model.Commit);
                memoryCache.UpdateCache();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            return Ok();
        }
    }
}
