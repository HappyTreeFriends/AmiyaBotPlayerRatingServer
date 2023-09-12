using System.ComponentModel.DataAnnotations;

namespace AmiyaBotPlayerRatingServer.Model
{
    public class ClientInfo
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string ClientId { get; set; }
        public string FriendlyName { get; set; }
        public string Description { get; set; }
        public string IconBase64 { get; set; }
        public string UserId { get; set; }
    }

}
