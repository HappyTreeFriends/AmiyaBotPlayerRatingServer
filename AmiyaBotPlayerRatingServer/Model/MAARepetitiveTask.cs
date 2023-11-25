using System.ComponentModel.DataAnnotations;
namespace AmiyaBotPlayerRatingServer.Model;
#pragma warning disable CS8618
// ReSharper disable UnusedMember.Global
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class MAARepetitiveTask
{

    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public String Name { get; set; } = "New Task";

    public bool IsPaused { get; set; } = false;
    public bool IsDeleted { get; set; } = false;

    public Guid ConnectionId { get; set; }
    public MAAConnection Connection { get; set; }

    public String Type { get; set; }
    public String Parameters { get; set; }

    public String? UtcCronString { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime AvailableFrom { get; set; } = DateTime.UtcNow;
    public DateTime? AvailableTo { get; set; }

    public DateTime? LastRunAt { get; set; }

    // 父子任务导航属性
    public virtual ICollection<MAATask> SubTasks { get; set; }
}