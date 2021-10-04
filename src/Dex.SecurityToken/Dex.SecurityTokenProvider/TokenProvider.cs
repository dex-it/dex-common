using System;
using System.Text.Json;
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

        public TokenProvider(ITokenInfoStorage tokenInfoStorage, IOptions<TokenProviderOptions> tokenProviderOptions)
        {
            _tokenInfoStorage = tokenInfoStorage ?? throw new ArgumentNullException(nameof(tokenInfoStorage));
            _tokenProviderOptions = tokenProviderOptions.Value;
            provider = DataProtectionProvider.Create(_tokenProviderOptions.ApplicationName);
        }


        public async Task<string> CreateTokenAsync<T>(T tokenModel) where T : BaseToken
        {
            if (tokenModel == null) throw new ArgumentNullException(nameof(tokenModel));
            if (tokenModel.Expired <= DateTimeOffset.Now) throw new ArgumentException("Expired date must be greater then current date ");

            var serializedToken = JsonSerializer.Serialize(tokenModel);

            var encryptedToken = provider.CreateProtector(nameof(T)).Protect(serializedToken);

            await _tokenInfoStorage.SaveTokenInfoAsync(new TokenInfo
            {
                Expired = tokenModel.Expired,
                Id = tokenModel.Id
            });
            return encryptedToken;
        }


        public async Task<string> CreateTokenUrlEscapedAsync<T>(T token) where T : BaseToken
        {
            return Uri.EscapeUriString(await CreateTokenAsync(token));
        }


        public async Task<T> GetTokenDataAsync<T>(string encryptedToken, bool throwIfInvalid = true) where T : BaseToken
        {
            if (string.IsNullOrEmpty(encryptedToken)) throw new ArgumentNullException(nameof(encryptedToken));

            var decryptedToken = provider.CreateProtector(nameof(T)).Unprotect(encryptedToken);

            var tokenData = JsonSerializer.Deserialize<T>(decryptedToken);

            var tokenInfo = await _tokenInfoStorage.GetTokenInfoAsync(tokenData.Id);

            var result = throwIfInvalid switch
            {
                true when tokenInfo.Expired <= DateTimeOffset.Now => throw new TokenExpiredException($"Id = {tokenInfo.Id}"),
                true when tokenInfo.Activated => throw new TokenAlreadyActivatedException($"Id = {tokenInfo.Id}"),
                true when _tokenProviderOptions.ApiResource != tokenData.Audience => throw new InvalidAudienceException(),
                _ => tokenData
            };
            await _tokenInfoStorage.SetActivatedAsync(tokenData.Id);
            return result;
        }
    }
}