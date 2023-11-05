using System.ComponentModel.DataAnnotations;

namespace AmiyaBotPlayerRatingServer.Model;

public class MAATask
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
        
    public Guid ConnectionId { get; set; }
    public MAAConnection Connection { get; set; }

    public bool IsCompleted { get; set; }

    public String Type { get; set; }

    public String? Parameters { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
}