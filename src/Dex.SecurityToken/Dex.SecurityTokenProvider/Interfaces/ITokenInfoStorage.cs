using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.SecurityTokenProvider.Exceptions;
using Dex.SecurityTokenProvider.Models;

namespace Dex.SecurityTokenProvider.Interfaces
{
    public interface ITokenInfoStorage
    {
        /// <summary>
        /// Get token info from storage
        /// </summary>
        /// <param name="tokenInfoId">id, identity key of Token</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="TokenInfoNotFoundException"></exception>
        Task<TokenInfo> GetTokenInfoAsync(Guid tokenInfoId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Save token info into storage
        /// </summary>
        /// <param name="tokenInfo"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="ArgumentNullException"></exception>
        Task SaveTokenInfoAsync(TokenInfo tokenInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Mark token as used
        /// </summary>
        /// <param name="tokenInfoId">identity key of token</param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="TokenInfoNotFoundException"></exception>
        Task SetActivatedAsync(Guid tokenInfoId, CancellationToken cancellationToken = default);
    }
}