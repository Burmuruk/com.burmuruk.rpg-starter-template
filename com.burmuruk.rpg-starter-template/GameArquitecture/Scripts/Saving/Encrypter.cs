using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Burmuruk.RPGStarterTemplate.Saving
{
    public static class Encrypter
    {
        static readonly byte[] key = new byte[16]
        {
            114,
            87,
            231,
            104,
            128,
            218,
            92,
            150,
            170,
            152,
            119,
            229,
            164,
            199,
            150,
            160
        };
        static readonly byte[] iv = new byte[16]
        {
            118,
            46,
            166,
            109,
            156,
            127,
            80,
            85,
            21,
            152,
            195,
            168,
            2,
            4,
            8,
            152
        };

        public static string EncryptString(JObject data)
        {
            var json = data.ToString();
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            var encryptedBytes = Encrypt(jsonBytes);
            return Convert.ToBase64String(encryptedBytes);
        }

        public static string DecryptString(string data)
        {
            var encryptedBytes = Convert.FromBase64String(data);
            var decryptedBytes = Decrypt(encryptedBytes);
            return Encoding.UTF8.GetString(decryptedBytes);
        }


        public static byte[] Encrypt(byte[] data)
        {
            using (Aes algorithm = Aes.Create())
            {
                algorithm.Padding = PaddingMode.PKCS7;
                using (ICryptoTransform cryptoTransform = algorithm.CreateEncryptor(key, iv))
                {
                    return Crypt(data, cryptoTransform);
                }
            }
        }

        public static byte[] Decrypt(byte[] data)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Padding = PaddingMode.PKCS7;
                using (ICryptoTransform decryptor = aes.CreateDecryptor(key, iv))
                {
                    return Crypt(data, decryptor);
                }
            }
        }

        private static byte[] Crypt(byte[] data, ICryptoTransform cryptoTransform)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream stream = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write))
                {
                    stream.Write(data, 0, data.Length);
                    stream.FlushFinalBlock();
                }

                return ms.ToArray();
            }
        }
    }
}
