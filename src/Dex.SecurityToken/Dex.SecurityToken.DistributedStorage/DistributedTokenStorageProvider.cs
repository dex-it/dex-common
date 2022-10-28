using Dex.SecurityTokenProvider.Exceptions;
using Dex.SecurityTokenProvider.Interfaces;
using Dex.SecurityTokenProvider.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace Dex.SecurityToken.DistributedStorage
{
    // ReSharper disable once UnusedType.Global
    /// <summary>
    /// Implementation of ITokenInfoStorage based on IDistributedCache 
    /// </summary>
    internal class DistributedTokenStorageProvider : ITokenInfoStorage
    {
        private readonly IDistributedCacheTypedClient _cacheTypedClient;

        public DistributedTokenStorageProvider(IDistributedCacheTypedClient cacheTypedClient)
        {
            _cacheTypedClient = cacheTypedClient ?? throw new ArgumentNullException(nameof(cacheTypedClient));
        }

        public async Task<TokenInfo> GetTokenInfoAsync(Guid tokenInfoId, CancellationToken cancellationToken = default)
        {
            if (tokenInfoId == default) throw new ArgumentNullException(nameof(tokenInfoId));

            return await _cacheTypedClient.GetAsync<TokenInfo>(tokenInfoId.ToString(), cancellationToken) ??
                   throw new TokenInfoNotFoundException($"TokenInfoId = {tokenInfoId}");
        }

        public async Task SaveTokenInfoAsync(TokenInfo tokenInfo, CancellationToken cancellationToken = default)
        {
            if (tokenInfo == null) throw new ArgumentNullException(nameof(tokenInfo));

            await _cacheTypedClient.SetAsync(tokenInfo.Id.ToString(), tokenInfo,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromTicks(tokenInfo.Expired.Ticks) }, cancellationToken);
        }

        public async Task SetActivatedAsync(Guid tokenInfoId, CancellationToken cancellationToken = default)
        {
            if (tokenInfoId == default) throw new ArgumentNullException(nameof(tokenInfoId));

            var tokenInfo = await _cacheTypedClient.GetAsync<TokenInfo>(tokenInfoId.ToString(), cancellationToken);
            if (tokenInfo == null) throw new TokenInfoNotFoundException($"TokenInfoId = {tokenInfoId}");

            tokenInfo.Activated = true;

            await _cacheTypedClient.SetAsync(tokenInfo.Id.ToString(), tokenInfo,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromTicks(tokenInfo.Expired.Ticks) }, cancellationToken);
        }
    }
}