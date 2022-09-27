using System;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Ef;
using Dex.Cap.Outbox.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests
{
    public abstract class BaseTest
    {
        protected string DbName { get; } = "db_test_" + Guid.NewGuid().ToString("N");

        [OneTimeSetUp]
        public async Task Setup()
        {
            var db = new TestDbContext(DbName);
            await db.Database.MigrateAsync();
        }

        [OneTimeTearDown]
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
                    builder.SetMinimumLevel(LogLevel.Debug);
                })
                .AddScoped(_ => new TestDbContext(DbName))
                .AddOutbox<TestDbContext>();

            sc.AddOptions<OutboxOptions>().Configure(options => options.ProcessorDelay = TimeSpan.FromMilliseconds(100));

            return sc;
        }

        protected static async Task SaveChanges(IServiceProvider sp)
        {
            var db = sp.GetRequiredService<TestDbContext>();
            await db.SaveChangesAsync();
        }
    }
}