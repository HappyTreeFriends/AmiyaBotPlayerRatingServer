﻿using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;
using AmiyaBotPlayerRatingServer.GameLogic.SkinGuess;

namespace AmiyaBotPlayerRatingServer.GameLogic
{
    public static class GameManagerExtensions
    {
        public static void AddGameManagers(this IServiceCollection service)
        {
            service.AddSingleton<GameManagerFactory>();
            service.AddSingleton<SchulteGridGameManager>();
            service.AddSingleton<SkinGuessManager>();
        }
    }
}