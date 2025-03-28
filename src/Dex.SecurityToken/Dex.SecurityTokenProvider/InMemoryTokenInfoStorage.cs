using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Dex.SecurityTokenProvider.Exceptions;
using Dex.SecurityTokenProvider.Interfaces;
using Dex.SecurityTokenProvider.Models;

namespace Dex.SecurityTokenProvider
{
    /// <summary>
    /// Not for production use, hold token info into memory, not persisted
    /// </summary>
    internal class InMemoryTokenInfoStorage : ITokenInfoStorage
    {
        private readonly ConcurrentDictionary<Guid, TokenInfo> _tokenInfos = new();

        public Task<TokenInfo> GetTokenInfoAsync(Guid tokenInfoId, CancellationToken cancellationToken = default)
        {
            if (tokenInfoId == default) throw new ArgumentNullException(nameof(tokenInfoId));
            if (!_tokenInfos.ContainsKey(tokenInfoId)) throw new TokenInfoNotFoundException($"TokenInfoId = {tokenInfoId}");

            var tokenInfo = _tokenInfos[tokenInfoId];
            return Task.FromResult(tokenInfo);
        }

        public Task SaveTokenInfoAsync(TokenInfo tokenInfo, CancellationToken cancellationToken = default)
        {
            if (tokenInfo == null) throw new ArgumentNullException(nameof(tokenInfo));
            if (!_tokenInfos.TryAdd(tokenInfo.Id, tokenInfo))
            {
                throw new TokenAlreadyExistException($"tokenId:{tokenInfo.Id}");
            }

            return Task.CompletedTask;
        }

        public Task SetActivatedAsync(Guid tokenInfoId, CancellationToken cancellationToken = default)
        {
            if (tokenInfoId == default) throw new ArgumentNullException(nameof(tokenInfoId));
            if (!_tokenInfos.ContainsKey(tokenInfoId))
                throw new TokenInfoNotFoundException($"TokenInfoId = {tokenInfoId}");

            _tokenInfos[tokenInfoId].Activated = true;
            return Task.CompletedTask;
        }
    }
}