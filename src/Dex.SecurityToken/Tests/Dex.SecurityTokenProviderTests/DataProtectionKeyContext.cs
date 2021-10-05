using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Dex.SecurityTokenProviderTests
{
    internal class DataProtectionKeyContext : DbContext, IDataProtectionKeyContext
    {
#pragma warning disable 8618
        public DataProtectionKeyContext(DbContextOptions<DataProtectionKeyContext> options) : base(options)
#pragma warning restore 8618
        {
        }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
    }
}