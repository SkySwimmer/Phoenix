using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PGL_Launcher
{
    public static class AesUtil
    {
        /// <summary>
        /// Encrypts a byte array
        /// </summary>
        /// <param name="payload">Data to encrypt</param>
        /// <param name="key">Security key</param>
        /// <returns>Encrypted bytes</returns>
        public static byte[] Encrypt(byte[] payload, byte[] key)
        {
            byte[] res;
            using (Aes aes = Aes.Create())
            {
                // Setup AES
                aes.Key = SHA256Managed.Create().ComputeHash(key);
                aes.IV = MD5.Create().ComputeHash(key);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Create encryptor
                var enc = aes.CreateEncryptor();

                // Create a memory stream for the encrypted data
                MemoryStream strm = new MemoryStream();

                // Create a encryping stream
                CryptoStream encStrm = new CryptoStream(strm, enc, CryptoStreamMode.Write);
                encStrm.Write(payload, 0, payload.Length);
                encStrm.Close();

                // Set the result object
                res = strm.ToArray();
            }
            return res;
        }

        /// <summary>
        /// Decrypts into a byte array
        /// </summary>
        /// <param name="payload">Encrypted payload</param>
        /// <param name="key">Encryption key</param>
        /// <returns>Decrypted byte array</returns>
        public static byte[] Decrypt(byte[] payload, byte[] key)
        {
            byte[] res;
            using (Aes aes = Aes.Create())
            {
                // Setup AES
                aes.Key = SHA256Managed.Create().ComputeHash(key);
                aes.IV = MD5.Create().ComputeHash(key);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Create decryptor
                var dec = aes.CreateDecryptor();

                // Create memory stream for decryption
                MemoryStream strm = new MemoryStream();

                // Load encrypted payload into a memory stream
                MemoryStream inp = new MemoryStream(payload);

                // Create decryption stream
                CryptoStream decStrm = new CryptoStream(inp, dec, CryptoStreamMode.Read);
                decStrm.CopyTo(strm);
                decStrm.Close();
                inp.Close();

                res = strm.ToArray();
            }
            return res;
        }
    }
}
