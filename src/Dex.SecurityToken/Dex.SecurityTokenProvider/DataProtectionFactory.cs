using System;
using Dex.SecurityTokenProvider.Interfaces;
using Dex.SecurityTokenProvider.Options;
using Microsoft.AspNetCore.DataProtection;

namespace Dex.SecurityTokenProvider
{
    public class DataProtectionFactory : IDataProtectionFactory
    {
        private readonly TokenProviderOptions _tokenProviderOptions;
        public DataProtectionFactory(TokenProviderOptions tokenProviderOptions)
        {
            _tokenProviderOptions = tokenProviderOptions;
        }
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, IDataProtector> _providers = new();

        public IDataProtector GetDataProtector(string purpose)
        {
            if (string.IsNullOrEmpty(purpose)) throw new ArgumentNullException(nameof(purpose));
            if (_providers.ContainsKey(purpose)) return _providers[purpose];

            var protector = DataProtectionProvider.Create(_tokenProviderOptions.ApplicationName).CreateProtector(purpose);
            _providers.TryAdd(purpose, protector);
            return protector;
        }
    }
}