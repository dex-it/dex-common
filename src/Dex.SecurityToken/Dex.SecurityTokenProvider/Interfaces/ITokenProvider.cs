using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.SecurityTokenProvider.Models;

namespace Dex.SecurityTokenProvider.Interfaces
{
    public interface ITokenProvider
    {
        Task<string> CreateTokenAsync<T>(Action<T> action, TimeSpan timeout, CancellationToken cancellationToken = default) where T : BaseToken, new();

        Task<string> CreateTokenUrlEscapedAsync<T>(Action<T> action, TimeSpan timeout, CancellationToken cancellationToken = default)
            where T : BaseToken, new();

        Task<T> GetTokenDataAsync<T>(string encryptedToken, bool throwIfInvalid = true, CancellationToken cancellationToken = default)
            where T : BaseToken;

        Task<T> GetUnescapedTokenDataAsync<T>(string encryptedToken, bool throwIfInvalid = true,
            CancellationToken cancellationToken = default)
            where T : BaseToken;
    }
}