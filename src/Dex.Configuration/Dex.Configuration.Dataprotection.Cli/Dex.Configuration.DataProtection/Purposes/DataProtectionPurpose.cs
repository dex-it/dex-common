using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Dex.Configuration.DataProtection.Purposes
{
    /// <summary>
    /// Класс предоставляющий методы создания уникального Purpose.
    /// </summary>
    public abstract class DataProtectionPurpose<T> where T : struct, Enum
    {
        protected const string ApplicationNameWrong = "Неверное значение Application Name.";

        protected virtual Dictionary<T, Tuple<string, string>> Constants { get; } =
            new Dictionary<T, Tuple<string, string>>();

        /// <summary>
        /// Вычислить Purpose.
        /// </summary>
        public string ComputePurpose(string applicationName)
        {
            if (!Enum.TryParse(applicationName, false, out T applicationNameEnum))
            {
                throw new InvalidOperationException(ApplicationNameWrong);
            }

            Constants.TryGetValue(applicationNameEnum, out var constantValues);

            if (constantValues == null)
            {
                throw new InvalidOperationException(ApplicationNameWrong);
            }

            return GenerateHash(constantValues.Item1, constantValues.Item2,
                CalculateSalt(applicationNameEnum, constantValues));
        }

        /// <summary>
        /// Вычислить соль.
        /// </summary>
        protected abstract string CalculateSalt(T applicationName, Tuple<string, string> constantValues);

        /// <summary>
        /// Сгенерировать SHA256 значение.
        /// </summary>
        private static string GenerateHash(string constValue1, string constValue2, string salt)
        {
            var combinedValue = $"{constValue1}{constValue2}{salt}";

            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(combinedValue);
                var hashedBytes = sha256.ComputeHash(bytes);
                var hashedValue = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();

                return hashedValue;
            }
        }
    }
}