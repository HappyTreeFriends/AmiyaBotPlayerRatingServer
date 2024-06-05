using System.Text;
using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.GameLogic;
using AmiyaBotPlayerRatingServer.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace AmiyaBotPlayerRatingServer.Controllers.Game
{
    [ApiController]
    [Route("api/gameHub")]
    public class GameHubController(
        PlayerRatingDatabaseContext dbContext,
        IConfiguration configuration,
        GameManager gameManager)
        : ControllerBase
    {

        private object GetGameReturnObj(GameLogic.Game game)
        {
            var creator = dbContext.Users.Find(game.CreatorId);

            var manager = gameManager.CreateGameManager(game.GameType);
            var payload = manager.GetGamePayload(game);

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
                game.PlayerList,
                game.RoomSettings,
                Payload = payload
            };
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpGet("{gameId}")]
        public async Task<IActionResult> GetGame(String gameId)
        {
            var game = await gameManager.GetGameAsync(gameId);
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
            var allGameInfos = dbContext.GameInfos.Where(g=>g.IsClosed==false).ToList();
            var allGames = new List<GameLogic.Game>();
            foreach (var gameInfo in allGameInfos)
            {
                var game = await gameManager.GetGameAsync(gameInfo.Id);
                if(game==null) continue;

                if (game is { IsPrivate: false, IsCompleted: false })
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
            var game = await gameManager.GetGameAsync(gameId);
            if (game == null)
            {
                return NotFound();
            }
            
            var shortenUrl = "https://game.anonymous-test.top/#/regular-home/room-waiting/" + game.Id;

            // HTTP Access
            var kuttUrl = configuration["Kutt:Url"];
            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://"+kuttUrl+"/api/v2/links");
            request.Content = new StringContent(JsonConvert.SerializeObject(new
            {
                target = shortenUrl,
                expire_in = "1 days",
                reuse = true
            }), Encoding.UTF8, "application/json");
            request.Headers.Add("X-API-KEY", configuration["Kutt:ApiKey"]);

            var response = await httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseJson = JsonConvert.DeserializeObject<dynamic>(responseContent);

            //{ "id":"a0d9d575-a405-4ee5-a3ee-9998ee6adda7","address":"ShortCode","description":null,"banned":false,"password":false,"expire_in":"2024-05-26T04:46:21.375Z","target":"https://game.anonymous-test.top/#/regular-home/room-waiting/7e9b9930-8934-40c7-ba7c-7de805c8f571","visit_count":0,"created_at":"2024-05-25T04:46:22.280Z","updated_at":"2024-05-25T04:46:22.280Z","link":"https://kutt.anonymous-test.top/YCPD0E"}

            var links = responseJson?.link?.ToString();

            links = links?.Replace("kutt.anonymous-test.top", "amiya.cn");

            return Ok(new
            {
                url=links
            });
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpGet("player/{userId}/statistics")]
        public async Task<IActionResult> GetPlayerStatistics(string userId)
        {
            var user = await dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            
            var stat = dbContext.ApplicationUserMinigameStatistics.FirstOrDefault(s => s.UserId == userId);
            if (stat == null)
            {
                return Ok(new
                {
                    TotalGamesPlayed = 0,
                    TotalGamesFirstPlace = 0,
                    TotalGamesSecondPlace = 0,
                    TotalGamesThirdPlace = 0,
                    TotalAnswersCorrect = 0,
                    TotalAnswersWrong = 0
                });
            }

            return Ok(new
            {
                stat.TotalGamesPlayed,
                stat.TotalGamesFirstPlace,
                stat.TotalGamesSecondPlace,
                stat.TotalGamesThirdPlace,
                stat.TotalAnswersCorrect,
                stat.TotalAnswersWrong
            });
        }

        [AllowAnonymous]
        [HttpGet("server-statistics")]
        public async Task<IActionResult> GetServerStatistics()
        {
            var totalPlaying =await dbContext.GameInfos.Where(c => c.IsClosed != true).CountAsync();
            var totalGames = await dbContext.GameInfos.CountAsync();
            var validPlayers = await dbContext.ApplicationUserMinigameStatistics.CountAsync();

            return Ok(new
            {
                GamesPlaying = totalPlaying,
                GamesTotal = totalGames,
                PlayersTotal = validPlayers
            });
        }
    }
}
