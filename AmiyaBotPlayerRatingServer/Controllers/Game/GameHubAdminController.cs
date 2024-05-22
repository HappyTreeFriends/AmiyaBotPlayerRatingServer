using System.Text;
using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.GameLogic;
using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AmiyaBotPlayerRatingServer.Controllers.Game
{
    [ApiController]
    [Route("api/gameHub")]
    public class GameHubController : ControllerBase
    {

        private readonly PlayerRatingDatabaseContext _dbContext;
        private readonly IConfiguration _configuration;


        public GameHubController(PlayerRatingDatabaseContext dbContext,IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

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

        private object GetGameReturnObj(GameLogic.Game game)
        {
            var creator = _dbContext.Users.Find(game.CreatorId);

            return new
            {
                game.Id,
                game.GameType,
                game.JoinCode,
                game.CreatorId,
                game.CreatorConnectionId,
                CreatorAvatar = creator?.Avatar,
                CreatorNickname = creator?.Nickname,
                game.CreateTime,
                game.IsStarted,
                game.StartTime,
                game.IsCompleted,
                game.CompleteTime,
                game.IsClosed,
                game.CloseTime,
                game.IsPrivate,
                game.PlayerList
            };
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpGet("{gameId}")]
        public IActionResult GetGame(String gameId)
        {
            var game = GameManager.GetGame(gameId);
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
            var list = GameManager.GameList.Where(g => g.IsPrivate == false && g.IsCompleted == false).Select(GetGameReturnObj);
            return Ok(list);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpGet("{gameId}/url")]
        public IActionResult GenerateShortenUrl(string gameId)
        {
            var game = GameManager.GetGame(gameId);
            if (game == null)
            {
                return NotFound();
            }

            /*

            https://kutt.anonymous-test.top/links
            X-API-KEY
            {
  "target": "string",
  "description": "string",
  "expire_in": "2 minutes/hours/days",
  "password": "string",
  "customurl": "string",
  "reuse": false,
  "domain": "string"
}
            
             */

            var shortenUrl = "https://game.anonymous-test.top/#/regular-home/room-waiting/" + game.Id;

            // HTTP Access
            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://kutt.anonymous-test.top/links");
            request.Content = new StringContent(JsonConvert.SerializeObject(new
            {
                target = shortenUrl,
                expire_in = "1 days",
                reuse = true
            }), Encoding.UTF8, "application/json");
            request.Headers.Add("X-API-KEY", _configuration["Kutt:ApiKey"]);


            return Ok();
        }
    }
}
