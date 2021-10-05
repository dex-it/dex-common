using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.SecurityTokenProvider.Models;

namespace Dex.SecurityTokenProvider.Interfaces
{
    public interface ITokenInfoStorage  
    {
        Task<TokenInfo> GetTokenInfoAsync(Guid tokenInfoId, CancellationToken cancellationToken = default);
        Task SaveTokenInfoAsync(TokenInfo tokenInfo, CancellationToken cancellationToken = default);
        Task SetActivatedAsync(Guid tokenInfoId);
    }
}