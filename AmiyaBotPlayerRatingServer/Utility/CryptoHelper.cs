using System.Security.Cryptography;
using System.Text;

namespace AmiyaBotPlayerRatingServer.Utility
{
    public static class CryptoHelper
    {
        public static string GetMd5Hash(string input)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                // 将输入字符串转换成字节数组并计算哈希数据
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                // 创建一个StringBuilder来收集字节并创建字符串
                StringBuilder sBuilder = new StringBuilder();

                // 遍历每个字节的数据 
                // 并将每个字节格式化为十六进制字符串
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                // 返回十六进制字符串
                return sBuilder.ToString();
            }
        }

    }
}
