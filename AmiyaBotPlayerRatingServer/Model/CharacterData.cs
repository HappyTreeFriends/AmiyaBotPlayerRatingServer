using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiyaBotPlayerRatingServer.Model;

#pragma warning disable CS8618
// ReSharper disable UnusedMember.Global
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class CharacterData
{
    [Key]
    public String Id { get; set; }

    /// <summary>
    /// 干员Id
    /// </summary>
    public string CharacterId { get; set; }

    public string CharacterName { get; set; }
}
