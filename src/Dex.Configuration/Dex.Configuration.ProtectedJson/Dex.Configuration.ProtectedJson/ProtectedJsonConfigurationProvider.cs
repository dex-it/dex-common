using System;
using System.IO;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration.Json;

namespace Dex.Configuration.ProtectedJson
{
    /// <summary>
    /// Расширенный провайдер <see cref="JsonConfigurationProvider"/> позволяющий работать с конфигурацией содержащие шифрованные данные.
    /// </summary>
    public class ProtectedJsonConfigurationProvider : JsonConfigurationProvider
    {
        private readonly ProtectedJsonConfigurationSource _protectedJsonConfigurationSource;

        /// <inheritdoc/>
        public ProtectedJsonConfigurationProvider(ProtectedJsonConfigurationSource source) : base(source)
        {
            _protectedJsonConfigurationSource = source;
        }

        /// <inheritdoc/>
        public override void Load(Stream stream)
        {
            base.Load(stream);

            var dataProtectionKeysDirectory = Data["ConfigurationProtectionOptions:KeysDirectory"];
            var dataProtectionApplicationName = Data["ConfigurationProtectionOptions:ApplicationName"];

            if (string.IsNullOrEmpty(dataProtectionKeysDirectory) ||
                string.IsNullOrEmpty(dataProtectionApplicationName))
            {
                throw new InvalidOperationException("Файл конфигураций не имеет необходимых значений для защиты");
            }

            var dataProtector =
                _protectedJsonConfigurationSource.DataProtectorFactory(dataProtectionApplicationName,
                    dataProtectionKeysDirectory);

            foreach (var key in _protectedJsonConfigurationSource.ProtectedKeys)
            {
                Data[key] = dataProtector.Unprotect(Data[key] ?? throw new InvalidOperationException());
            }
        }
    }
}