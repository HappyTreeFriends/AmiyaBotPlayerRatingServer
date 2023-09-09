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

            credential.RefreshedAt = DateTime.UtcNow;
            credential.RefreshSuccess = false;

            var httpClient = _httpClientFactory.CreateClient();

            if (string.IsNullOrEmpty(credential.Credential))
            {
                return;
            }

            httpClient.DefaultRequestHeaders.Add("Cred", credential.Credential);

            var response = await httpClient.GetAsync("https://zonai.skland.com/api/v1/user/me");
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return;
            }

            var content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(content))
            {
                return;
            }

            var jObject = JObject.Parse(content);

            if ((int?)jObject["code"] != 0)
            {
                return;
            }

            var meData = jObject["data"];
            var uid = meData?["gameStatus"]?["uid"]?.ToString();

            if (string.IsNullOrEmpty(uid))
            {
                return;
            }

            response = await httpClient.GetAsync($"https://zonai.skland.com/api/v1/game/player/info?uid={uid}");

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return;
            }

            content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(content))
            {
                return;
            }

            jObject = JObject.Parse(content);

            if ((int?)jObject["code"] != 0)
            {
                return;
            }

            var infoData = jObject["data"];
            var statusData = infoData?["status"];

            credential.SKLandUid = uid;
            credential.Nickname = statusData?["name"]?.ToString()??"Unknown";
            var charName = statusData?["secretary"]?["skinId"]?.ToString();
            if (!string.IsNullOrEmpty(charName))
            {
                credential.AvatarUrl = $@"https://web.hycdn.cn/arknights/game/assets/char_skin/avatar/{Uri.EscapeDataString(charName)}.png";
            }

            var charBoxJson = JsonConvert.SerializeObject(infoData?["chars"]);

            if (!string.IsNullOrEmpty(charBoxJson))
            {
                var charBox = new SKLandCharacterBox()
                {
                    Id = Guid.NewGuid().ToString(),
                    CredentialId = credential.Id,
                    CharacterBoxJson = charBoxJson
                };

                _dbContext.SKLandCharacterBoxes.Add(charBox);
            }

            credential.RefreshSuccess = true;

            await _dbContext.SaveChangesAsync();
        }

    }
}
