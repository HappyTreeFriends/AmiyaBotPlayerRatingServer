using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;
using AmiyaBotPlayerRatingServer.GameLogic.SkinGuess;
using AmiyaBotPlayerRatingServer.RealtimeHubs;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;
using static AmiyaBotPlayerRatingServer.GameLogic.Game;

namespace AmiyaBotPlayerRatingServer.GameLogic
{
    public interface IGameManager
    {
        public Task<Game> CreateNewGame(Dictionary<String, JToken> param);

        public Task<object> HandleMove(Game game, string playerId, string move);

        public Task<object> GetGamePayload(Game game);
        public Task<object> GetGameStartPayload(Game game);
        public Task<object> GetCloseGamePayload(Game game);

        public Task<double> GetScore(Game game, string player);

        public class RequestHintOrGiveUpResult
        {
            public object Payload { get; set; }
            public bool HintTriggered { get; set; }
            public bool GiveUpTriggered { get; set; }
        }

        public Task<RequestHintOrGiveUpResult> GiveUp(Game game, string appUserId);
        public Task<RequestHintOrGiveUpResult> RequestHint(Game game, string appUserId);
    }
}
