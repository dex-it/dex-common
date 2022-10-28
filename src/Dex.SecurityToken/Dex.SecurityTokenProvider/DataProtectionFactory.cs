using System.Collections.Concurrent;
using Dex.SecurityTokenProvider.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace Dex.SecurityTokenProvider
{
    /// <summary>
    /// Allow to create and cache IDataProtector.
    /// Can be singltone.
    /// </summary>
    public class DataProtectionFactory : IDataProtectionFactory
    {
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly ConcurrentDictionary<string, Lazy<IDataProtector>> _protectors = new();

        public DataProtectionFactory(IDataProtectionProvider dataProtectionProvider)
        {
            _dataProtectionProvider = dataProtectionProvider ?? throw new ArgumentNullException(nameof(dataProtectionProvider));
        }

        public IDataProtector GetDataProtector(string purpose)
        {
            if (string.IsNullOrEmpty(purpose)) throw new ArgumentNullException(nameof(purpose));

            // ReSharper disable once HeapView.CanAvoidClosure
            return _protectors.GetOrAdd(purpose, s => new Lazy<IDataProtector>(() => _dataProtectionProvider.CreateProtector(s))).Value;
        }
    }
}