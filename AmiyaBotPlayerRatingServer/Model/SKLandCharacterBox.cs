#pragma warning disable CS8618
namespace AmiyaBotPlayerRatingServer.Model
{
    public class SKLandCharacterBox
    {
        public string Id { get; set; }
        public string CredentialId { get; set; }  // ForeignKey to SKLandCredential
        public string CharacterBoxJson { get; set; }  // 角色列表（character box） in JSON

        public DateTime RefreshedAt { get; set; } //表示最后一次刷新的时间

        public SKLandCredential Credential { get; set; }  // Navigation Property
    }

}
