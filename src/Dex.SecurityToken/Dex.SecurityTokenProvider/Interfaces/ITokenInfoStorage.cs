using System;
using System.Threading.Tasks;
using Dex.SecurityTokenProvider.Models;

namespace Dex.SecurityTokenProvider.Interfaces
{
    public interface ITokenInfoStorage  
    {
        Task<TokenInfo> GetTokenInfoAsync(Guid tokenInfoId);
        Task SaveTokenInfoAsync(TokenInfo tokenInfo);
        Task SetActivatedAsync(Guid tokenInfoId);
    }
}