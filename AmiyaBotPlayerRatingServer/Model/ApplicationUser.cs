using Microsoft.AspNetCore.Identity;
namespace AmiyaBotPlayerRatingServer.Model;

#pragma warning disable CS8618
// ReSharper disable UnusedMember.Global
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class ApplicationUser : IdentityUser
{
    public string Nickname { get; set; }
}

