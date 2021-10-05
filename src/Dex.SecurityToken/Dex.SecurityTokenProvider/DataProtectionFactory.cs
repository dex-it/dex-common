﻿using System;
using System.Collections.Concurrent;
using Dex.SecurityTokenProvider.Interfaces;
using Dex.SecurityTokenProvider.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Dex.SecurityTokenProvider
{
    public class DataProtectionFactory : IDataProtectionFactory
    {
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly ConcurrentDictionary<string, IDataProtector> _protectors = new();

        public DataProtectionFactory(IOptions<TokenProviderOptions> tokenProviderOptions)
        {
            if (tokenProviderOptions.Value == null) throw new ArgumentNullException(nameof(tokenProviderOptions));
            _dataProtectionProvider = DataProtectionProvider.Create(tokenProviderOptions.Value.ApplicationName);
        }

        public IDataProtector GetDataProtector(string purpose)
        {
            if (string.IsNullOrEmpty(purpose)) throw new ArgumentNullException(nameof(purpose));

            // ReSharper disable once HeapView.CanAvoidClosure
            return _protectors.GetOrAdd(purpose,  s => _dataProtectionProvider.CreateProtector(s));
        }
    }
}