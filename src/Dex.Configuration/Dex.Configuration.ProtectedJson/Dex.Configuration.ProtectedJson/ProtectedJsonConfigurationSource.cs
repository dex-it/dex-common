using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace Dex.Configuration.ProtectedJson
{
    /// <summary>
    /// Класс описывающий источник JSON конфигурации.
    /// </summary>
    public class ProtectedJsonConfigurationSource : JsonConfigurationSource
    {
        /// <summary>
        /// Идентификаторы ключей конфигурации, значения которых содержат шифрованные данные.
        /// </summary>
        public IEnumerable<string> ProtectedKeys { get; set; }

        /// <summary>
        /// Шифратор/расшифровщик данных.
        /// </summary>
        public CreateDataProtectorDelegate DataProtectorFactory { get; set; }

        /// <inheritdoc/>
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            ResolveFileProvider();
            EnsureDefaults(builder);
            return new ProtectedJsonConfigurationProvider(this);
        }
    }
}