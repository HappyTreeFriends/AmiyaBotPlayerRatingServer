using AmiyaBotPlayerRatingServer.Data;
using Newtonsoft.Json.Linq;

namespace AmiyaBotPlayerRatingServer.GameLogic.CypherChallenge
{
    public class CypherChallengeManager(ArknightsMemoryCache memoryCache, PlayerRatingDatabaseContext dbContext)
        : IGameManager
    {
        
        private Game? GenerateGame()
        {
            var operatorNames = memoryCache.GetObject<Dictionary<String,String>>("character_names.json");
            var operatorList = memoryCache.GetJson("operator_archive.json");

            if (operatorNames == null || operatorList == null)
            {
                return null;
            }

            //随机选择一个
            var random = new Random();
            var operatorIdList = operatorNames.Keys.ToList();
            var randomIndex = random.Next(0, operatorIdList.Count);
            var operatorId = operatorIdList[randomIndex];
            var randomOperator = operatorList[operatorId];

            var game = new CypherChallengeGame
            {
                CharacterName = randomOperator["name"].ToString(),
                CharacterId = randomOperator["charId"].ToString(),
                NationAnswer = randomOperator["nation"].ToString(),
                ProfessionAnswer = randomOperator["profession"].ToString(),
                SubProfessionAnswer = randomOperator["subProfession"].ToString(),
                RarityAnswer = randomOperator["rarity"].ToString()

            };

            return game;
        }

        public Task<Game?> CreateNewGame(Dictionary<string, JToken> param)
        {
            throw new NotImplementedException();
        }

        public Task<object> HandleMove(Game game, string playerId, string move)
        {
            throw new NotImplementedException();
        }

        public Task<object> GetGamePayload(Game game)
        {
            throw new NotImplementedException();
        }

        public Task<object> GetGameStartPayload(Game game)
        {
            throw new NotImplementedException();
        }

        public Task<object> GetCloseGamePayload(Game game)
        {
            throw new NotImplementedException();
        }

        public Task<double> GetScore(Game game, string player)
        {
            throw new NotImplementedException();
        }

        public Task<IGameManager.RequestHintOrGiveUpResult> GiveUp(Game game, string appUserId)
        {
            throw new NotImplementedException();
        }

        public Task<IGameManager.RequestHintOrGiveUpResult> RequestHint(Game game, string appUserId)
        {
            throw new NotImplementedException();
        }

        public Task<object> GetCompleteGamePayload(Game game)
        {
            throw new NotImplementedException();
        }
    }
}
