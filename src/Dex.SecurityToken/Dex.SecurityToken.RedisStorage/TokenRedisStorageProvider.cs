using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.SecurityTokenProvider.Exceptions;
using Dex.SecurityTokenProvider.Interfaces;
using Dex.SecurityTokenProvider.Models;
using ServiceStack.Redis;

namespace Dex.SecurityToken.RedisStorage
{
    // ReSharper disable once UnusedType.Global
    /// <summary>
    /// Implementation of ITokenInfoStorage based on Redis
    /// </summary>
    public class TokenRedisStorageProvider : ITokenInfoStorage
    {
        private readonly IRedisClientsManagerAsync _redisClientsManager;

        public TokenRedisStorageProvider(IRedisClientsManagerAsync redisClientsManager)
        {
            _redisClientsManager = redisClientsManager ?? throw new ArgumentNullException(nameof(redisClientsManager));
        }

        public async Task<TokenInfo> GetTokenInfoAsync(Guid tokenInfoId, CancellationToken cancellationToken = default)
        {
            if (tokenInfoId == default) throw new ArgumentNullException(nameof(tokenInfoId));

            await using var redis = await _redisClientsManager.GetClientAsync(cancellationToken);

            return await redis.As<TokenInfo>().GetByIdAsync(tokenInfoId, cancellationToken) ??
                   throw new TokenInfoNotFoundException($"TokenInfoId = {tokenInfoId}");
        }

        public async Task SaveTokenInfoAsync(TokenInfo tokenInfo, CancellationToken cancellationToken = default)
        {
            if (tokenInfo == null) throw new ArgumentNullException(nameof(tokenInfo));

            await using var redis = await _redisClientsManager.GetClientAsync(cancellationToken);
            await redis.As<TokenInfo>()
                .StoreAsync(tokenInfo, TimeSpan.FromTicks(tokenInfo.Expired.Ticks), cancellationToken);
        }

        public async Task SetActivatedAsync(Guid tokenInfoId)
        {
            if (tokenInfoId == default) throw new ArgumentNullException(nameof(tokenInfoId));

            await using var redis = await _redisClientsManager.GetClientAsync();

            var tokenInfo = await redis.As<TokenInfo>().GetByIdAsync(tokenInfoId);
            if (tokenInfo == null) throw new TokenInfoNotFoundException($"TokenInfoId = {tokenInfoId}");

            tokenInfo.Activated = true;

            await redis.As<TokenInfo>()
                .StoreAsync(tokenInfo, TimeSpan.FromTicks(tokenInfo.Expired.Ticks));
        }
    }
}