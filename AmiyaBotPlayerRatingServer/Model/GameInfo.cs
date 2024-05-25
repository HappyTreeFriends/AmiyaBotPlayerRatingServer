using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

#pragma warning disable CS8618
// ReSharper disable UnusedMember.Global
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
namespace AmiyaBotPlayerRatingServer.Model
{
    [Index(nameof(JoinCode), Name = "Index_JoinCode")]
    [Index(nameof(IsClosed), Name = "Index_IsClosed")]
    [Index(nameof(CreatorId), Name = "Index_CreatorId")]
    public class GameInfo
    {
        [Key]
        public string Id { get; set; }

        public string GameType { get; set; }
        
        public string CreatorId { get; set; }
        public ApplicationUser Creator { get; set; }
        
        public string JoinCode{ get; set; }
        public bool IsClosed { get; set; }

        public ICollection<ApplicationUser> PlayerList { get; set; } = new List<ApplicationUser>();

    }
}
