using System;
using System.Threading.Tasks;
using Dex.Cap.OnceExecutor.Ef.Extensions;
using Dex.Cap.Outbox.Ef.Extensions;
using Dex.Cap.Outbox.Options;
using Dex.Cap.Outbox.RetryStrategies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests
{
    public abstract class BaseTest
    {
        protected string DbName { get; } = "db_test_" + DateTime.Now.Ticks;

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

        protected IServiceCollection InitServiceCollection(int messageToProcessLimit = 10, int parallelLimit = 2,
            Action<OutboxRetryStrategyConfigurator>? strategyConfigure = null)
        {
            var serviceCollection = new ServiceCollection();
            AddLogging(serviceCollection);

            serviceCollection
                .AddScoped(_ => new TestDbContext(DbName))
                .AddOutbox<TestDbContext, TestDiscriminator>((provider, configurator) => { strategyConfigure?.Invoke(configurator); })
                .AddOnceExecutor<TestDbContext>()
                .AddOptions<OutboxOptions>()
                .Configure(options =>
                {
                    options.MessagesToProcess = messageToProcessLimit;
                    options.ConcurrencyLimit = parallelLimit;
                });

            return serviceCollection;
        }

        protected static void AddLogging(IServiceCollection serviceCollection)
        {
            serviceCollection.AddLogging(builder =>
            {
                builder.AddDebug();
                builder.AddProvider(new TestLoggerProvider());
                builder.SetMinimumLevel(LogLevel.Trace);
            });
        }

        protected static TestDbContext GetDb(IServiceProvider sp)
        {
            return sp.GetRequiredService<TestDbContext>();
        }

        protected static async Task SaveChanges(IServiceProvider sp)
        {
            await GetDb(sp).SaveChangesAsync();
        }
    }
}