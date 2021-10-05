using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.SecurityTokenProvider.Exceptions;
using Dex.SecurityTokenProvider.Interfaces;
using Dex.SecurityTokenProvider.Models;
using ServiceStack.Redis;

namespace TokenInfoRedisStorageProvider
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

        /// <summary>
        /// Get token info from storage
        /// </summary>
        /// <param name="tokenInfoId">id, identity key of Token</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="TokenInfoNotFoundException"></exception>
        public async Task<TokenInfo> GetTokenInfoAsync(Guid tokenInfoId, CancellationToken cancellationToken = default)
        {
            if (tokenInfoId == default) throw new ArgumentNullException(nameof(tokenInfoId));

            await using var redis = await _redisClientsManager.GetClientAsync(cancellationToken);

            return await redis.As<TokenInfo>().GetByIdAsync(tokenInfoId, cancellationToken) ??
                   throw new TokenInfoNotFoundException($"TokenInfoId = {tokenInfoId}");
        }

        /// <summary>
        /// Save token info into storage
        /// </summary>
        /// <param name="token"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task SaveTokenInfoAsync(TokenInfo token, CancellationToken cancellationToken = default)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));

            await using var redis = await _redisClientsManager.GetClientAsync(cancellationToken);
            await redis.As<TokenInfo>()
                .StoreAsync(token, TimeSpan.FromTicks(token.Expired.Ticks), cancellationToken);
        }

        /// <summary>
        /// Mark token as used
        /// </summary>
        /// <param name="tokenInfoId">identity key of token</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="TokenInfoNotFoundException"></exception>
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