#pragma warning disable CS8618
namespace AmiyaBotPlayerRatingServer.Model
{
    public class SKLandCharacterBox
    {
        public int Id { get; set; }
        public int CredentialId { get; set; }  // ForeignKey to SKLandCredential
        public string CharacterBoxJson { get; set; }  // 角色列表（character box） in JSON

        public SKLandCredential Credential { get; set; }  // Navigation Property
    }

}
