namespace AmiyaBotPlayerRatingServer.Model
{
#pragma warning disable CS8618
    // ReSharper disable UnusedMember.Global
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class SystemNotification
    {
        public Guid Id { get; set; }
        public string Message { get; set; }
        public DateTime ExpiredAt { get; set; }
    }
}
