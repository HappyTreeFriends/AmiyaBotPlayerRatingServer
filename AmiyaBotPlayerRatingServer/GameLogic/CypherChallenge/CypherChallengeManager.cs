using AmiyaBotPlayerRatingServer.Data;
using Newtonsoft.Json.Linq;

namespace AmiyaBotPlayerRatingServer.GameLogic.CypherChallenge
{
    public class CypherChallengeManager(ArknightsMemoryCache memoryCache, PlayerRatingDatabaseContext dbContext)
        : IGameManager
    {
        
        private Game? GenerateGame()
        {
            var operatorList = memoryCache.GetJson("character_table_full.json");

            //随机选择一个
            var random = new Random();
            var randomIndex = random.Next(0, operatorList.Count());
            var randomOperator = operatorList[randomIndex];

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
    }
}
