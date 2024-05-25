namespace AmiyaBotPlayerRatingServer.Model
{
    #pragma warning disable CS8618
    // ReSharper disable UnusedMember.Global
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class ApplicationUserMinigameStatistics
    {
        public string Id { get; set; }

        public ApplicationUser User { get; set; }
        public string UserId { get; set; }

        public int TotalGamesPlayed { get; set; }

        public int TotalGamesFirstPlace { get; set; }
        public int TotalGamesSecondPlace { get; set; }
        public int TotalGamesThirdPlace { get; set; }

        public int TotalAnswersCorrect { get; set; }
        public int TotalAnswersWrong { get; set; }
    }
}
