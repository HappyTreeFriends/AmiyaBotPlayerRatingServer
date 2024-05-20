using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AmiyaBotPlayerRatingServer.Model;
#pragma warning disable CS8618
// ReSharper disable UnusedMember.Global
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class SKLandCredential
{
    [Key]
    public string Id { get; set; }
    public string UserId { get; set; } // ForeignKey to ApplicationUser
    public ApplicationUser User { get; set; } // Navigation Property

    public string Credential { get; set; } // SKLand Credential string

    public string SKLandUid { get; set; } // SKLand中的Uid
    public string Nickname { get; set; } // SKLand昵称
    public string AvatarUrl { get; set; } // SKLand头像URL

    public DateTime RefreshedAt { get; set; } //表示最后一次刷新的时间
    public bool RefreshSuccess { get; set; } //表示最后一次刷新是否成功
}


