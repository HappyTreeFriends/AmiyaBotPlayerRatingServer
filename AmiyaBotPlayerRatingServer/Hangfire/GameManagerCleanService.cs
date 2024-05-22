using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.GameLogic;

namespace AmiyaBotPlayerRatingServer.Hangfire
{
    public class GameManagerCleanService
    {
        private readonly GameManager _gameManager;
        
        public GameManagerCleanService(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        public async Task Clean()
        {
            var completedTimeout = _gameManager.GameList.Where(x => x.IsClosed == false && x.IsCompleted && (
                DateTime.Now - x.CompleteTime > new TimeSpan(0, 1, 0, 0)));

            var startedTimeout = _gameManager.GameList.Where(x => x.IsStarted && (
                DateTime.Now - x.StartTime > new TimeSpan(1, 0, 0, 0)));

            var allTimeout = completedTimeout.Concat(startedTimeout).Distinct().ToList();

            foreach (var game in allTimeout)
            {
                game.IsClosed = true;
                game.CloseTime = DateTime.Now;
            }
        }
    }
}
