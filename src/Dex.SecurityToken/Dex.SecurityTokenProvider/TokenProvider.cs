using System.Text.Json;
using Dex.SecurityTokenProvider.Exceptions;
using Dex.SecurityTokenProvider.Interfaces;
using Dex.SecurityTokenProvider.Models;
using Dex.SecurityTokenProvider.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Dex.SecurityTokenProvider
{
    /// <summary>
    /// 
    /// </summary>
    public class TokenProvider : ITokenProvider
    {
        private readonly ITokenInfoStorage _tokenInfoStorage;
        private readonly TokenProviderOptions _tokenProviderOptions;
        private readonly IDataProtectionFactory _dataProtectionFactory;

        public TokenProvider(ITokenInfoStorage tokenInfoStorage, IDataProtectionFactory dataProtectionFactory,
            IOptions<TokenProviderOptions> tokenProviderOptions)
        {
            _tokenInfoStorage = tokenInfoStorage ?? throw new ArgumentNullException(nameof(tokenInfoStorage));
            _dataProtectionFactory = dataProtectionFactory ?? throw new ArgumentNullException(nameof(dataProtectionFactory));
            _tokenProviderOptions = tokenProviderOptions.Value ?? throw new ArgumentNullException(nameof(tokenProviderOptions));
        }


        public async Task<string> CreateTokenAsync<T>(Action<T>? action, TimeSpan timeout, CancellationToken cancellationToken = default)
            where T : BaseToken, new()
        {
            if (timeout <= TimeSpan.FromSeconds(0)) throw new ArgumentException("Timeout date must be greater then zero");

            var token = new T
            {
                Audience = _tokenProviderOptions.ApiResource,
                Expired = DateTimeOffset.UtcNow.Add(timeout)
            };

            action?.Invoke(token);

            var serializedToken = JsonSerializer.Serialize(token);

            await _tokenInfoStorage.SaveTokenInfoAsync(new TokenInfo
            {
                Expired = token.Expired,
                Id = token.Id
            }, cancellationToken);

            return _dataProtectionFactory.GetDataProtector(nameof(T)).Protect(serializedToken);
        }

        public async Task<T> GetTokenDataAsync<T>(string encryptedToken, bool throwIfInvalid = true,
            CancellationToken cancellationToken = default) where T : BaseToken
        {
            if (string.IsNullOrEmpty(encryptedToken)) throw new ArgumentNullException(nameof(encryptedToken));
            return await GetTokenData<T>(throwIfInvalid, encryptedToken, cancellationToken);
        }

        public async Task<string> CreateTokenAsUrlAsync<T>(Action<T>? action, TimeSpan timeout, CancellationToken cancellationToken = default)
            where T : BaseToken, new()
        {
            return Uri.EscapeDataString(await CreateTokenAsync(action, timeout, cancellationToken));
        }

        public async Task<T> GetTokenDataFromUrlAsync<T>(string encryptedToken, bool throwIfInvalid = true, CancellationToken cancellationToken = default)
            where T : BaseToken
        {
            var unescapedToken = Uri.EscapeDataString(encryptedToken);
            return await GetTokenDataAsync<T>(unescapedToken, throwIfInvalid, cancellationToken);
        }

        public Task MarkTokenAsUsed(Guid tokenInfoId)
        {
            return _tokenInfoStorage.SetActivatedAsync(tokenInfoId);
        }


        private async Task<T> GetTokenData<T>(bool throwIfInvalid, string encryptedToken, CancellationToken cancellationToken = default)
            where T : BaseToken
        {
            var decryptedToken = _dataProtectionFactory.GetDataProtector(nameof(T)).Unprotect(encryptedToken);
            var tokenData = JsonSerializer.Deserialize<T>(decryptedToken);
            var tokenInfo = await _tokenInfoStorage.GetTokenInfoAsync(tokenData!.Id, cancellationToken);

            var result = throwIfInvalid switch
            {
                true when tokenInfo.Expired <= DateTimeOffset.UtcNow => throw new TokenExpiredException($"Id = {tokenInfo.Id}"),
                true when tokenInfo.Activated => throw new TokenAlreadyActivatedException($"Id = {tokenInfo.Id}"),
                true when _tokenProviderOptions.ApiResource != tokenData.Audience => throw new TokenInvalidAudienceException(),
                _ => tokenData
            };
            return result;
        }
    }
}