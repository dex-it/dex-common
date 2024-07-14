using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dex.Cap.Ef.Tests.SqliteProviderTests;

public class SqliteTestDbContextFactory: IDesignTimeDbContextFactory<SqliteTestDbContext>
{
    public SqliteTestDbContext CreateDbContext(string[] args)
    {
        return new SqliteTestDbContext(string.Empty);
    }
}