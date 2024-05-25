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
        private readonly GameManager _gameManager;


        public GameHubController(PlayerRatingDatabaseContext dbContext,IConfiguration configuration,GameManager gameManager)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _gameManager = gameManager;
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
            _gameManager.Notifications.Add(new SystemNotification
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
        public async Task<IActionResult> GetGame(String gameId)
        {
            var game = await _gameManager.GetGameAsync(gameId);
            if (game == null)
            {
                return NotFound();
            }

            return Ok(GetGameReturnObj(game));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpGet]
        public async Task<IActionResult> ListGame()
        {
            var allGameInfos = _dbContext.GameInfos.Where(g=>g.IsClosed==false).ToList();
            var allGames = new List<GameLogic.Game>();
            foreach (var gameInfo in allGameInfos)
            {
                var game = await _gameManager.GetGameAsync(gameInfo.Id);
                if (!game.IsPrivate&&!game.IsCompleted)
                {
                    allGames.Add(game);
                }
            }
            
            return Ok(allGames.Select(GetGameReturnObj));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpGet("{gameId}/url")]
        public async Task<IActionResult> GenerateShortenUrl(string gameId)
        {
            var game = await _gameManager.GetGameAsync(gameId);
            if (game == null)
            {
                return NotFound();
            }
            
            var shortenUrl = "https://game.anonymous-test.top/#/regular-home/room-waiting/" + game.Id;

            // HTTP Access
            var kuttUrl = _configuration["Kutt:Url"];
            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://"+ kuttUrl + "/api/v2/links");
            request.Content = new StringContent(JsonConvert.SerializeObject(new
            {
                target = shortenUrl,
                expire_in = "1 days",
                reuse = true
            }), Encoding.UTF8, "application/json");
            request.Headers.Add("X-API-KEY", _configuration["Kutt:ApiKey"]);

            var response = await httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseJson = JsonConvert.DeserializeObject<dynamic>(responseContent);

            //{ "id":"a0d9d575-a405-4ee5-a3ee-9998ee6adda7","address":"YCPD0E","description":null,"banned":false,"password":false,"expire_in":"2024-05-26T04:46:21.375Z","target":"https://game.anonymous-test.top/#/regular-home/room-waiting/7e9b9930-8934-40c7-ba7c-7de805c8f571","visit_count":0,"created_at":"2024-05-25T04:46:22.280Z","updated_at":"2024-05-25T04:46:22.280Z","link":"https://kutt.anonymous-test.top/YCPD0E"}

            var links = (responseJson.link)?.ToString();

            links = links.Replace("kutt.anonymous-test.top", "amiya.cn");

            return Ok(new
            {
                url=links
            });
        }
    }
}
