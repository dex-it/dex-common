using System;
using System.Threading.Tasks;
using Dex.Cap.OnceExecutor.Ef.Extensions;
using Dex.Cap.Outbox.Ef;
using Dex.Cap.Outbox.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests
{
    public abstract class BaseTest
    {
        protected string DbName { get; } = "db_test_" + Guid.NewGuid().ToString("N");

        [SetUp]
        public virtual async Task Setup()
        {
            var db = new TestDbContext(DbName);
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
            // await db.Database.MigrateAsync();
        }

        [TearDown]
        public async Task TearDown()
        {
            var db = new TestDbContext(DbName);
            await db.Database.EnsureDeletedAsync();
        }

        protected IServiceCollection InitServiceCollection()
        {
            var sc = new ServiceCollection()
                    .AddLogging(builder =>
                    {
                        builder.AddDebug();
                        builder.AddProvider(new TestLoggerProvider());
                        builder.SetMinimumLevel(LogLevel.Trace);
                    })
                    .AddScoped(_ => new TestDbContext(DbName))
                ;

            sc.AddOutbox<TestDbContext>().AddOptions<OutboxOptions>();
            sc.AddOnceExecutor<TestDbContext>();

            return sc;
        }

        protected static async Task SaveChanges(IServiceProvider sp)
        {
            var db = sp.GetRequiredService<TestDbContext>();
            await db.SaveChangesAsync();
        }
    }
}