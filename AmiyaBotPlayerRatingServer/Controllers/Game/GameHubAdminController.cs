using AmiyaBotPlayerRatingServer.GameLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmiyaBotPlayerRatingServer.Controllers.Game
{
    [ApiController]
    [Route("api/gameHubAdmin")]
    public class GameHubAdminController : ControllerBase
    {

        public class SendNotificationModel
        {
            public string Message { get; set; }
            public DateTime ExpiredAt { get; set; }
        }

        [Authorize(Roles = "管理员账户")]
        [HttpPost("sendNotificationToAll")]
        public Task SendNotificationToAll([FromBody] SendNotificationModel model)
        {
            GameManager.Notifications.Add(new SystemNotification
            {
                Id = Guid.NewGuid().ToString(),
                Message = model.Message,
                ExpiredAt = model.ExpiredAt
            });
            return Task.CompletedTask;
        }
    }
}
