using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dex.Audit.Persistence.DesignTimeContextFactory;

public class DesignTimeAuditContextFactory : IDesignTimeDbContextFactory<AuditContext>
{
    public AuditContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuditContext>();

        optionsBuilder.UseNpgsql(args[0],
            opts => opts.CommandTimeout((int)TimeSpan.FromMinutes(10).TotalSeconds));

        return new AuditContext(optionsBuilder.Options);
    }
}