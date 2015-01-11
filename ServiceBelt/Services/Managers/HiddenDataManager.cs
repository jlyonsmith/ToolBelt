using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace ServiceBelt
{
    public class HiddenDataManager : IHiddenDataManager
    {
        byte[] key;
        RijndaelManaged rm = new RijndaelManaged();
        Random random = new Random();

        public HiddenDataManager(string key)
        {
            this.key = Convert.FromBase64String(key);

            if (this.key.Length != 32)
                throw new ArgumentException("Key must be 44 characters (32 bytes) long");
        }

        public string Hide(string text)
        {
            return Convert.ToBase64String(Hide(Encoding.UTF8.GetBytes(text)));
        }

        public string Reveal(string hiddenText)
        {
            return Encoding.UTF8.GetString(Reveal(Convert.FromBase64String(hiddenText)));
        }

        public byte[] Hide(byte[] buffer)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                var vector = new byte[16];

                random.NextBytes(vector);
                ms.Write(vector, 0, vector.Length);

                ICryptoTransform encryptor = rm.CreateEncryptor(key, vector);

                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    var bytes = BitConverter.GetBytes(buffer.Length);

                    cs.Write(bytes, 0, bytes.Length);
                    cs.Write(buffer, 0, buffer.Length);
                }

                return ms.ToArray();
            }
        }

        public byte[] Reveal(byte[] hiddenBuffer)
        {
            using (MemoryStream ms = new MemoryStream(hiddenBuffer))
            {
                var vector = new byte[16];

                ms.Read(vector, 0, vector.Length);

                ICryptoTransform decryptor = rm.CreateDecryptor(key, vector);
                byte[] data;

                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                {
                    data = new byte[Marshal.SizeOf<int>()];
                    cs.Read(data, 0, data.Length);

                    data = new byte[BitConverter.ToInt32(data, 0)];
                    cs.Read(data, 0, data.Length);
                }

                return data;
            }
        }
    }
}

