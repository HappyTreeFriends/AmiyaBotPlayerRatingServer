using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiyaBotPlayerRatingServer.Model;
#pragma warning disable CS8618
// ReSharper disable UnusedMember.Global
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class ClientInfo
{

    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ClientId { get; set; }
    public string FriendlyName { get; set; }
    public string Description { get; set; }
    public string IconBase64 { get; set; }
    public string RedirectUri { get; set; }
    public string UserId { get; set; }
}


