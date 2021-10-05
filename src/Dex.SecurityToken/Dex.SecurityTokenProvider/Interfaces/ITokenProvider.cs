using System.Threading;
using System.Threading.Tasks;
using Dex.SecurityTokenProvider.Models;

namespace Dex.SecurityTokenProvider.Interfaces
{
    public interface ITokenProvider
    {
        Task<string> CreateTokenAsync<T>(T tokenModel, int timeoutSeconds = 1, CancellationToken cancellationToken = default) where T : BaseToken;

        Task<string> CreateTokenUrlEscapedAsync<T>(T token) where T : BaseToken;

        Task<T> GetTokenDataAsync<T>(string encryptedToken, int timeoutSeconds = 1, bool throwIfInvalid = true, CancellationToken cancellationToken = default)
            where T : BaseToken;

        Task<T> GetUnescapedTokenDataAsync<T>(string encryptedToken, int timeoutSeconds = 1, bool throwIfInvalid = true,
            CancellationToken cancellationToken = default)
            where T : BaseToken;
    }
}