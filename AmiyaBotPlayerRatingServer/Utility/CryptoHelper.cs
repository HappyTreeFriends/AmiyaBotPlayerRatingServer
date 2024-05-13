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

        public static string GeneratePassword(int length)
        {
            const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string specialChars = "!@#$%^&*()-+";

            Random random = new Random();
            StringBuilder password = new StringBuilder();
            List<string> charCategories = new List<string>
            {
                upperCase, lowerCase, digits, specialChars
            };

            // Ensure at least one character from two different categories
            password.Append(upperCase[random.Next(upperCase.Length)]);
            password.Append(specialChars[random.Next(specialChars.Length)]);

            // Fill the rest of the password length
            while (password.Length < length)
            {
                string selectedCategory = charCategories[random.Next(charCategories.Count)];
                password.Append(selectedCategory[random.Next(selectedCategory.Length)]);
            }

            // Shuffle the generated password to randomize character positions
            string shuffledPassword = ShuffleString(password.ToString(), random);
            return shuffledPassword;
        }

        private static string ShuffleString(string input, Random rng)
        {
            char[] array = input.ToCharArray();
            int n = array.Length;
            while (n > 1)
            {
                int k = rng.Next(n--);
                char temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
            return new String(array);
        }
    }
}
