using Dex.SecurityTokenProvider.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace Dex.SecurityTokenProvider
{
    public class DataProtectionFactory : IDataProtectionFactory
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, IDataProtectionProvider> _providers = new();

        public IDataProtectionProvider GetDataProtection(string appName)
        {
            if (_providers.ContainsKey(appName)) return _providers[appName];

            var provider = DataProtectionProvider.Create(appName);
            _providers.TryAdd(appName, provider);
            return provider;
        }
    }
}