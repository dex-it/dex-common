using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Dex.Configuration.DataProtection.DataEncryption
{
    /// <summary>
    /// Класс, осуществляющий шифрование и расшифрование данных.
    /// </summary>
    public static class DataEncryptor
    {
        private const string CertificateResourceName =
            "Dex.Configuration.DataProtection.DataEncryption.DataProtectionContainer.pfx";

        private static readonly Guid CertGuid = Guid.Parse("3215D7D7-C8CA-4E9A-AFCE-A6F628E0F4E0");
        private static readonly X509Certificate2 Certificate;

        /// <summary>
        /// Конструктор.
        /// </summary>
        static DataEncryptor()
        {
            var byteGuid = CertGuid.ToByteArray();
            var pass = Convert.ToBase64String(byteGuid);

            using (var resourceStream =
                   Assembly.GetExecutingAssembly().GetManifestResourceStream(CertificateResourceName)
                   ?? throw new FileNotFoundException(CertificateResourceName))
            {
                var raw = new byte[resourceStream.Length];

                for (var i = 0; i < resourceStream.Length; ++i)
                {
                    raw[i] = (byte)resourceStream.ReadByte();
                }

                Certificate = new X509Certificate2(raw, pass);
            }
        }

        /// <summary>
        /// Расшифровать данные.
        /// </summary>
        /// <param name="encryptedData">Зашифрованные данные в виде base64-строки.</param>
        /// <returns>Расшифрованные данные.</returns>
        public static string Decrypt(string encryptedData)
        {
            var encryptedBytes = Convert.FromBase64String(encryptedData);
            byte[] decryptedBytes;

            using (RSA rsa = Certificate.GetRSAPrivateKey() ?? throw new ArgumentNullException(nameof(rsa)))
            {
                decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.OaepSHA256);
            }

            return Encoding.UTF8.GetString(decryptedBytes);
        }

        /// <summary>
        /// Зашифрованные данные.
        /// </summary>
        /// <param name="data">Не зашифрованные данные.</param>
        /// <returns>Зашифрованные данные в виде base64-строки.</returns>
        public static string Encrypt(string data)
        {
            var dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] encryptedBytes;

            using (RSA rsa = Certificate.GetRSAPublicKey() ?? throw new ArgumentNullException(nameof(rsa)))
            {
                encryptedBytes = rsa.Encrypt(dataBytes, RSAEncryptionPadding.OaepSHA256);
            }

            return Convert.ToBase64String(encryptedBytes);
        }
    }
}