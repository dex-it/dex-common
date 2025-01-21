using Microsoft.AspNetCore.DataProtection;

namespace Dex.Configuration.ProtectedJson
{
    /// <summary>
    /// Делегат создания <see cref="IDataProtector"/>.
    /// </summary>
    public delegate IDataProtector CreateDataProtectorDelegate(string applicationName, string keysDirectory);
}
