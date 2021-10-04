using System.Threading.Tasks;
using Dex.SecurityTokenProvider.Models;

namespace Dex.SecurityTokenProvider.Interfaces
{
    public interface ITokenProvider 
    {
        Task<string> CreateTokenAsync<T>(T tokenModel) where T : BaseToken;

        Task<string> CreateTokenUrlEscapedAsync<T>(T token) where T : BaseToken;

        Task<T> GetTokenDataAsync<T>(string encryptedToken, bool throwIfInvalid = true) where T : BaseToken;
    }
   
}