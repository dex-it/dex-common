using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.SecurityTokenProvider.Exceptions;
using Dex.SecurityTokenProvider.Interfaces;
using Dex.SecurityTokenProvider.Models;
using ServiceStack.Redis;

namespace TokenInfoRedisStorageProvider
{
    public class TokenRedisStorageProvider : ITokenInfoStorage 
    {
        private readonly IRedisClientsManagerAsync _redisClientsManager;

        public TokenRedisStorageProvider(IRedisClientsManagerAsync redisClientsManager)
        {
            _redisClientsManager = redisClientsManager;
        }

        public async Task<TokenInfo> GetTokenInfoAsync(Guid tokenInfoId, CancellationToken cancellationToken = default)
        {
            if (tokenInfoId == default) throw new ArgumentNullException(nameof(tokenInfoId));

            await using var redis = await _redisClientsManager.GetClientAsync();

            return await redis.As<TokenInfo>().GetByIdAsync(tokenInfoId) ?? throw new TokenInfoNotFoundException($"TokenInfoId = {tokenInfoId}");
        }


        public async Task SaveTokenInfoAsync(TokenInfo token, CancellationToken cancellationToken = default)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));
            await using var redis = await _redisClientsManager.GetClientAsync(cancellationToken);
            await redis.As<TokenInfo>()
                .StoreAsync(token, TimeSpan.FromTicks(token.Expired.Ticks), cancellationToken);
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