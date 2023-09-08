using AmiyaBotPlayerRatingServer.Data;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using AmiyaBotPlayerRatingServer.Model;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace AmiyaBotPlayerRatingServer.Hangfire
{
    public class CollectPlayerInformationService
    {
        private readonly IConfiguration _configuration;
        private readonly PlayerRatingDatabaseContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;

        public CollectPlayerInformationService(IConfiguration configuration, PlayerRatingDatabaseContext dbContext, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
        }

        public async Task Collect(String credentialId)
        {
            if (string.IsNullOrEmpty(credentialId))
            {
                return;
            }

            var credential = await _dbContext.SKLandCredentials.Where(x => x.Id == credentialId).FirstOrDefaultAsync();

            if (credential == null)
            {
                return;
            }

            credential.RefreshedAt = DateTime.Now.ToUniversalTime();
            credential.RefreshSuccess = false;

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Cred", credential.Credential);

            var response = await httpClient.GetAsync("https://zonai.skland.com/api/v1/user/me");
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return;
            }

            var content = await response.Content.ReadAsStringAsync();
            var jObject = JObject.Parse(content);
            if ((int)jObject["code"] != 0)
            {
                return;
            }

            var meData = jObject["data"];
            var uid = meData["gameStatus"]["uid"].ToString();

            response = await httpClient.GetAsync($"https://zonai.skland.com/api/v1/game/player/info?uid={uid}");

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return;
            }

            content = await response.Content.ReadAsStringAsync();
            jObject = JObject.Parse(content);

            if ((int)jObject["code"] != 0)
            {
                return;
            }

            var infoData = jObject["data"];

            var statusData = infoData["status"];

            credential.Nickname = statusData["name"]?.ToString();
            var charname = statusData["secretary"]["skinId"]?.ToString();
            credential.AvatarUrl = $@"https://web.hycdn.cn/arknights/game/assets/char_skin/avatar/{charname.Replace("#", "%23").Replace("@", "%40")}.png";


            var charBox = new SKLandCharacterBox()
            {
                Id = Guid.NewGuid().ToString(),
                CredentialId = credential.Id,
                CharacterBoxJson = JsonConvert.SerializeObject(infoData["chars"])
            };

            _dbContext.SKLandCharacterBoxes.Add(charBox);
            

            credential.RefreshSuccess = true;

            await _dbContext.SaveChangesAsync();
        }
    }
}
