using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace AmiyaBotPlayerRatingServer.Utility
{
    public class OpenIddictKeyManager
    {
        private const string KeyFileName = "EncryptedOpenIddictKey.bin";
        private readonly IConfiguration _configuration;

        public OpenIddictKeyManager(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void GenerateKeys()
        {
            // 生成新的 RSA 密钥
            var rsa = RSA.Create();
            rsa.KeySize = 2048;

            // 导出并加密
            byte[] plainKey = rsa.ExportRSAPrivateKey();
            byte[] encryptedKey = EncryptData(plainKey, _configuration["JWT:Secret"]);

            var filename = Path.Combine(Directory.GetCurrentDirectory(), "Resources", KeyFileName);

            // 保存到文件
            File.WriteAllBytes(filename, encryptedKey);
        }

        public RsaSecurityKey GetKeys()
        {
            RSA? rsa=null;

            var filename = Path.Combine(Directory.GetCurrentDirectory(), "Resources", KeyFileName);

            if (File.Exists(filename))
            {
                // 密钥文件已存在，读取并解密
                byte[] encryptedKey = File.ReadAllBytes(filename);
                byte[] plainKey = DecryptData(encryptedKey, _configuration["JWT:Secret"]);
                rsa = RSA.Create();
                rsa.ImportRSAPrivateKey(plainKey, out _);
            }

            return new RsaSecurityKey(rsa);
        }

        private byte[] EncryptData(byte[] dataToEncrypt, string? password)
        {
            if (password == null)
            {
                return Array.Empty<byte>();
            }
            using Aes aes = Aes.Create();
            var key = new Rfc2898DeriveBytes(password, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, 1000, 
                HashAlgorithmName.SHA1);
            aes.Key = key.GetBytes(aes.KeySize / 8);
            aes.IV = key.GetBytes(aes.BlockSize / 8);

            using MemoryStream ms = new MemoryStream();
            using CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(dataToEncrypt, 0, dataToEncrypt.Length);
            cs.Close();
            return ms.ToArray();
        }

        private byte[] DecryptData(byte[] dataToDecrypt, string? password)
        {
            if (password == null)
            {
                return Array.Empty<byte>();
            }

            using Aes aes = Aes.Create();
            var key = new Rfc2898DeriveBytes(password, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, 1000, 
                HashAlgorithmName.SHA1);
            aes.Key = key.GetBytes(aes.KeySize / 8);
            aes.IV = key.GetBytes(aes.BlockSize / 8);

            using MemoryStream ms = new MemoryStream();
            using CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(dataToDecrypt, 0, dataToDecrypt.Length);
            cs.Close();
            return ms.ToArray();
        }
    }

}
