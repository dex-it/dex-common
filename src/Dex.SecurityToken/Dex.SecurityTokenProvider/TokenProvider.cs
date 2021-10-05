using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dex.SecurityTokenProvider.Exceptions;
using Dex.SecurityTokenProvider.Interfaces;
using Dex.SecurityTokenProvider.Models;
using Dex.SecurityTokenProvider.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Dex.SecurityTokenProvider
{
    public class TokenProvider : ITokenProvider
    {
        private readonly ITokenInfoStorage _tokenInfoStorage;
        private readonly TokenProviderOptions _tokenProviderOptions;

        private readonly IDataProtectionProvider provider;

        public TokenProvider(ITokenInfoStorage tokenInfoStorage, IOptions<TokenProviderOptions> tokenProviderOptions,
            IDataProtectionFactory dataProtectionFactory)
        {
            _tokenInfoStorage = tokenInfoStorage ?? throw new ArgumentNullException(nameof(tokenInfoStorage));
            _tokenProviderOptions = tokenProviderOptions.Value;
            provider = dataProtectionFactory.GetDataProtection(_tokenProviderOptions.ApplicationName);
        }


        public async Task<string> CreateTokenAsync<T>(T tokenModel, int timeoutSeconds = 1, CancellationToken cancellationToken = default) where T : BaseToken
        {
            if (tokenModel == null) throw new ArgumentNullException(nameof(tokenModel));
            if (tokenModel.Expired <= DateTimeOffset.UtcNow) throw new ArgumentException("Expired date must be greater then current date ");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                return await Task.Run(async () =>
                {
                    tokenModel.Audience ??= _tokenProviderOptions.ApiResource;
                    tokenModel.Id = tokenModel.Id != default ? tokenModel.Id : Guid.NewGuid();

                    var serializedToken = JsonSerializer.Serialize(tokenModel);

                    var encryptedToken = provider.CreateProtector(nameof(T)).Protect(serializedToken);

                    await _tokenInfoStorage.SaveTokenInfoAsync(new TokenInfo
                    {
                        Expired = tokenModel.Expired,
                        Id = tokenModel.Id
                    });
                    return encryptedToken;
                }, cts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"Timeout exceeded {timeoutSeconds} sec");
            }
        }


        public async Task<string> CreateTokenUrlEscapedAsync<T>(T token) where T : BaseToken
        {
            return Uri.EscapeUriString(await CreateTokenAsync(token));
        }


        public async Task<T> GetTokenDataAsync<T>(string encryptedToken, int timeoutSeconds = 1, bool throwIfInvalid = true,
            CancellationToken cancellationToken = default) where T : BaseToken
        {
            if (string.IsNullOrEmpty(encryptedToken)) throw new ArgumentNullException(nameof(encryptedToken));

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            try
            {
                return await Task.Run(async () => await GetTokenData<T>(throwIfInvalid, encryptedToken), cts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"Timeout exceeded {timeoutSeconds} sec");
            }
        }

        public async Task<T> GetUnescapedTokenDataAsync<T>(string encryptedToken, int timeoutSeconds = 1, bool throwIfInvalid = true,
            CancellationToken cancellationToken = default) where T : BaseToken
        {
            var unescapedToken = Uri.EscapeUriString(encryptedToken);
            return await GetTokenDataAsync<T>(unescapedToken, timeoutSeconds, throwIfInvalid, cancellationToken);
        }


        private async Task<T> GetTokenData<T>(bool throwIfInvalid, string encryptedToken) where T : BaseToken
        {
            var decryptedToken = provider.CreateProtector(nameof(T)).Unprotect(encryptedToken);

            var tokenData = JsonSerializer.Deserialize<T>(decryptedToken);

            var tokenInfo = await _tokenInfoStorage.GetTokenInfoAsync(tokenData!.Id);

            var result = throwIfInvalid switch
            {
                true when tokenInfo.Expired <= DateTimeOffset.UtcNow => throw new TokenExpiredException($"Id = {tokenInfo.Id}"),
                true when tokenInfo.Activated => throw new TokenAlreadyActivatedException($"Id = {tokenInfo.Id}"),
                true when _tokenProviderOptions.ApiResource != tokenData.Audience => throw new InvalidAudienceException(),
                _ => tokenData
            };
            return result;
        }
    }
}