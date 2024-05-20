using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace AmiyaBotPlayerRatingServer.Model;
#pragma warning disable CS8618
// ReSharper disable UnusedMember.Global
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class MAATask
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid ConnectionId { get; set; }
    public MAAConnection Connection { get; set; }
    
    public bool IsCompleted { get; set; }
    public bool IsSystemGenerated { get; set; }

    public String Type { get; set; } = "CaptureImage";
    public String? Parameters { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime AvailableAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // 父MAARepetitiveTask导航属性
    public Guid? ParentRepetitiveTaskId { get; set; }
    public virtual MAARepetitiveTask? ParentRepetitiveTask { get; set; }

    // 父子任务导航属性
    public virtual ICollection<MAATask> SubTasks { get; set; }
    public Guid? ParentTaskId { get; set; }
    public virtual MAATask? ParentTask { get; set; }
}