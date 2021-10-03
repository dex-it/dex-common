using System;
using System.Threading.Tasks;
using Dex.SecurityTokenProvider.Models;

namespace Dex.SecurityTokenProvider.Interfaces
{
    public interface ITokenInfoStorage  
    {
        Task<TokenInfo> GetTokenInfoAsync(Guid id);
        Task SaveTokenInfoAsync(TokenInfo token);
        Task SetActivated(Guid tokenDataId);
    }
}