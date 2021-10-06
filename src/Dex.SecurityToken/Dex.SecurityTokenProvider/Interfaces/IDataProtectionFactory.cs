using Microsoft.AspNetCore.DataProtection;

namespace Dex.SecurityTokenProvider.Interfaces
{
    public interface IDataProtectionFactory
    {
        /// <summary>
        /// Create or get IDataProtector insntance by string key
        /// </summary>
        /// <param name="purpose">key purpose</param>
        /// <returns>IDataProtector</returns>
        IDataProtector GetDataProtector(string purpose);
    }
}