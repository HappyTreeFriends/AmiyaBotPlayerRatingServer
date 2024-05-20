using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace AmiyaBotPlayerRatingServer.Model;
#pragma warning disable CS8618
// ReSharper disable UnusedMember.Global
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class MAAConnection
{

    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 用户给出的友好名称
    /// </summary>
    public string? Name { get; set; }

    public string UserId { get; set; }
    public ApplicationUser User { get; set; }

    public String? DeviceIdentity { get; set; }
    public String UserIdentity { get; set; }
}

