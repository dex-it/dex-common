using System;
using System.IO;
using Dex.Configuration.DataProtection.DataEncryption;
using Dex.Configuration.DataProtection.Purposes;
using Microsoft.AspNetCore.DataProtection;

namespace Dex.Configuration.DataProtection
{
    /// <summary>
    /// Класс предоставляющий методы работы с <see cref="IDataProtector"/>.
    /// </summary>
    public static class DataProtection
    {
        /// <summary>
        /// Зашифровать данные.
        /// </summary>
        /// <param name="keysDirectory">Директория хранения ключей.</param>
        /// <param name="applicationName">Имя приложения.</param>
        /// <param name="projectName">Имя проекта.</param>
        /// <param name="plaintext">Входные данные для шифрования.</param>
        /// <returns>Зашифрованные данные.</returns>
        public static string ProtectData(
            DirectoryInfo keysDirectory,
            string applicationName,
            string projectName,
            string plaintext)
        {
            var calculatedPurpose = ComputePurpose(applicationName, projectName);

            var dataProtector = CreateProtector(keysDirectory, calculatedPurpose);
            var protectedData = dataProtector.Protect(plaintext);

            return protectedData;
        }

        /// <summary>
        /// Расшифровать данные.
        /// </summary>
        /// <param name="keysDirectory">Директория хранения ключей.</param>
        /// <param name="applicationName">Имя приложения.</param>
        /// <param name="projectName">Имя проекта.</param>
        /// <param name="protectedData">Входные данные для расшифрования.</param>
        /// <returns>Расшифрованные данные.</returns>
        public static string UnprotectData(
            DirectoryInfo keysDirectory,
            string applicationName,
            string projectName,
            string protectedData)
        {
            var calculatedPurpose = ComputePurpose(applicationName, projectName);

            var dataProtector = CreateProtector(keysDirectory, calculatedPurpose);
            var plaintext = dataProtector.Unprotect(protectedData);

            return plaintext;
        }

        /// <summary>
        /// Перешифровать данные.
        /// </summary>
        /// <param name="keysDirectory">Директория хранения ключей.</param>
        /// <param name="applicationName">Имя приложения.</param>
        /// <param name="projectName">Имя проекта.</param>
        /// <param name="encryptedData">Входные зашифрованные данные.</param>
        /// <returns>Расшифрованные данные.</returns>
        public static string ProtectEncryptedData(
            DirectoryInfo keysDirectory,
            string applicationName,
            string projectName,
            string encryptedData)
        {
            var plaintext = DataEncryptor.Decrypt(encryptedData);

            var calculatedPurpose = ComputePurpose(applicationName, projectName);

            var dataProtector = CreateProtector(keysDirectory, calculatedPurpose);
            var protectedData = dataProtector.Protect(plaintext);

            return protectedData;
        }

        /// <summary>
        /// Создать <see cref="IDataProtector"/>.
        /// </summary>
        /// <param name="keysDirectory">Директория хранения ключей.</param>
        /// <param name="purpose">Назначение, которое должно быть присвоено вновь создаваемому <see cref="T:Microsoft.AspNetCore.DataProtection.IDataProtector"/></param>
        /// <returns><see cref="IDataProtector"/>, привязанный к предоставленному назначению.</returns>
        private static IDataProtector CreateProtector(DirectoryInfo keysDirectory, string purpose)
        {
            var dataProtectionProvider = DataProtectionProvider.Create(keysDirectory);

            return dataProtectionProvider.CreateProtector(purpose);
        }

        private static string ComputePurpose(string applicationName, string projectName)
        {
            if (!Enum.TryParse(projectName, false, out ProjectName enumProjectName))
            {
                throw new InvalidOperationException("Неверное значение Project Name.");
            }

            switch (enumProjectName)
            {
                case ProjectName.Template:
                    return new TemplateDataProtectionPurpose().ComputePurpose(applicationName);
                default:
                    throw new InvalidOperationException("Неверное значение Project Name.");
            }
        }
    }
}