using System.ComponentModel.DataAnnotations;

namespace AmiyaBotPlayerRatingServer.Model;

public class MAAResponse
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TaskId { get; set; }
    public MAATask Task { get; set; }

    public String Payload { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}