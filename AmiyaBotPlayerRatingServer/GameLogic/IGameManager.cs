using System.Diagnostics.CodeAnalysis;
using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;
using AmiyaBotPlayerRatingServer.GameLogic.SkinGuess;
using AmiyaBotPlayerRatingServer.RealtimeHubs;
using JetBrains.Annotations;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;
using static AmiyaBotPlayerRatingServer.GameLogic.Game;

namespace AmiyaBotPlayerRatingServer.GameLogic
{
    public interface IGameManager
    {
#pragma warning disable CS8618
        [SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
        public class RequestHintOrGiveUpResult
        {
            public object Payload { get; set; }
            public bool HintTriggered { get; set; }
            public bool GiveUpTriggered { get; set; }
        }
#pragma warning restore CS8618

        public Task<object> GetGamePayload(Game rawGame);

        public Task<Game?> CreateNewGame(Dictionary<String, object> param);
        public Task<object> GetGameStartPayload(Game game);
        public Task<double> GetScore(Game game, string player);

        public Task<object> HandleMove(Game game, string playerId, string move);
        public Task<RequestHintOrGiveUpResult> GiveUp(Game game, string appUserId);
        public Task<RequestHintOrGiveUpResult> RequestHint(Game game, string appUserId);

        public Task<object> GetCompleteGamePayload(Game game);
        public Task<object> GetCloseGamePayload(Game game);




    }
}
