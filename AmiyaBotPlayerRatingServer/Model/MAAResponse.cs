using System.ComponentModel.DataAnnotations;
namespace AmiyaBotPlayerRatingServer.Model;
#pragma warning disable CS8618
// ReSharper disable UnusedMember.Global
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class MAAResponse
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid TaskId { get; set; }
    public MAATask Task { get; set; }

    public String Payload { get; set; }

    public byte[]? ImagePayload { get; set; }
    public byte[]? ImagePayloadThumbnail { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}