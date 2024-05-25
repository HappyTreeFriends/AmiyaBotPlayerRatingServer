using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;
using AmiyaBotPlayerRatingServer.GameLogic.SkillGuess;
using AmiyaBotPlayerRatingServer.GameLogic.SkinGuess;

namespace AmiyaBotPlayerRatingServer.GameLogic
{
    public static class GameManagerExtensions
    {
        public static void AddGameManagers(this IServiceCollection service)
        {
            service.AddScoped<GameManager>();
            service.AddScoped<SchulteGridGameManager>();
            service.AddScoped<SkinGuessManager>();
            service.AddScoped<SkillGuessManager>();
        }
    }
}
