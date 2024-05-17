using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;
using AmiyaBotPlayerRatingServer.GameLogic.SkinGuess;

namespace AmiyaBotPlayerRatingServer.GameLogic
{
    public class GameManagerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public GameManagerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public GameManager CreateGameManager(string gameType)
        {
            return gameType switch
            {
                "SchulteGrid" => _serviceProvider!.GetService<SchulteGridGameManager>()!,
                "SkinGuess" => _serviceProvider!.GetService<SkinGuessManager>()!,
                _ => throw new ArgumentException("Invalid game type"),
            };
        }
    }
}
