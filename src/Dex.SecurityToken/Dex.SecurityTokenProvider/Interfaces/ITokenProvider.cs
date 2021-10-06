using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.SecurityTokenProvider.Models;

namespace Dex.SecurityTokenProvider.Interfaces
{
    public interface ITokenProvider
    {
        /// <summary>
        /// Create token and save TokenInfo to ITokenInfoStorage, return string token representation 
        /// </summary>
        /// <param name="action">delegate to fill token custom data</param>
        /// <param name="timeout">time to life</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T">Generic type of token, for customer data saving</typeparam>
        /// <returns>string token representation</returns>
        /// <exception cref="ArgumentException"></exception>
        Task<string> CreateTokenAsync<T>(Action<T> action, TimeSpan timeout, CancellationToken cancellationToken = default) where T : BaseToken, new();

        /// <summary>
        /// Decode token data from string token representation, deserialize to T: TokenType
        /// </summary>
        /// <param name="encryptedToken">string token data</param>
        /// <param name="throwIfInvalid">flag, if true, throw exception if any condition has failed</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T">user token type</typeparam>
        /// <returns>user token instance</returns>
        /// <exception cref="ArgumentNullException"></exception>
        Task<T> GetTokenDataAsync<T>(string encryptedToken, bool throwIfInvalid = true, CancellationToken cancellationToken = default)
            where T : BaseToken;

        /// <summary>
        /// Create token and save TokenInfo to ITokenInfoStorage, return escaped string token representation 
        /// </summary>
        /// <param name="action">delegate to fill token custom data</param>
        /// <param name="timeout">time to life</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T">Generic type of token, for customer data saving</typeparam>
        /// <returns>escaped string token representation</returns>
        /// <exception cref="ArgumentException"></exception>
        Task<string> CreateTokenAsUrlAsync<T>(Action<T> action, TimeSpan timeout, CancellationToken cancellationToken = default)
            where T : BaseToken, new();

        /// <summary>
        /// Decode token data from escped string token representation, deserialize to T: TokenType
        /// </summary>
        /// <param name="encryptedToken">escaped string token data</param>
        /// <param name="throwIfInvalid">flag, if true, throw exception if any condition has failed</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T">user token type</typeparam>
        /// <returns>user token instance</returns>
        /// <exception cref="ArgumentNullException"></exception>
        Task<T> GetTokenDataFromUrlAsync<T>(string encryptedToken, bool throwIfInvalid = true,
            CancellationToken cancellationToken = default)
            where T : BaseToken;

        /// <summary>
        /// Marks the token used
        /// </summary>
        /// <param name="tokenInfoId"></param>
        /// <returns></returns>
        Task MarkTokenAsUsed(Guid tokenInfoId);
    }
}