using Microsoft.AspNetCore.DataProtection;

namespace Dex.SecurityTokenProvider.Interfaces
{
    public interface IDataProtectionFactory
    {   
        IDataProtectionProvider GetDataProtection(string appName);
    }
}