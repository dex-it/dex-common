using Microsoft.AspNetCore.DataProtection;

namespace Dex.SecurityTokenProvider.Interfaces
{
    public interface IDataProtectionFactory
    {   
        IDataProtector GetDataProtector(string purpose);
    }
}