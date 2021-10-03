using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dex.SecurityTokenProvider.Exceptions;
using Dex.SecurityTokenProvider.Interfaces;
using Dex.SecurityTokenProvider.Models;

namespace Dex.SecurityTokenProviderTests
{
    internal class TestTokenInfoStorage : ITokenInfoStorage
    {
        private readonly Dictionary<Guid, TokenInfo> _tokenInfos = new();

        public Task<TokenInfo> GetTokenInfoAsync(Guid id)
        {
            if (id == default) throw new ArgumentNullException(nameof(id));
            var tokenInfo = _tokenInfos[id];
            return Task.FromResult(tokenInfo ?? throw new TokenInfoNotFoundException($"TokenInfoId = {id}"));
        }

        public Task SaveTokenInfoAsync(TokenInfo token)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));
            _tokenInfos.Add(token.Id, token);
            return Task.CompletedTask;
        }

        public Task SetActivated(Guid tokenId)
        {
            if (tokenId == default) throw new ArgumentNullException(nameof(tokenId));
            _tokenInfos[tokenId].Activated = true;
            return Task.CompletedTask;
        }
    }
}