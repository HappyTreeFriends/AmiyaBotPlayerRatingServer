using System.ComponentModel.DataAnnotations;

namespace AmiyaBotPlayerRatingServer.Model
{
    public class MAAConnection
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public String? DeviceIdentity { get; set; }
        public String UserIdentity { get; set; }
    }
}
