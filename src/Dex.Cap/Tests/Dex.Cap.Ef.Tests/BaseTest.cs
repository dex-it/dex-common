using System;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.OutboxTests.Handlers;
using Dex.Cap.OnceExecutor.Ef.Extensions;
using Dex.Cap.Outbox;
using Dex.Cap.Outbox.Ef.Extensions;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;
using Dex.Cap.Outbox.Options;
using Dex.Outbox.Command.Test;
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
            var serviceCollection = new ServiceCollection();
            AddLogging(serviceCollection);

            serviceCollection
                .AddScoped(_ => new TestDbContext(DbName))
                .AddOutbox<TestDbContext>()
                .AddOnceExecutor<TestDbContext>()
                .AddOptions<OutboxOptions>();

            serviceCollection.AddSingleton<IOutboxTypeDiscriminator, TestDiscriminator>();

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

        protected static async Task SaveChanges(IServiceProvider sp)
        {
            var db = sp.GetRequiredService<TestDbContext>();
            await db.SaveChangesAsync();
        }
    }

    internal class TestDiscriminator : BaseOutboxTypeDiscriminator
    {
        public TestDiscriminator()
        {
            Add(nameof(EmptyOutboxMessage), typeof(EmptyOutboxMessage).AssemblyQualifiedName!);

            Add(nameof(TestUserCreatorCommand), typeof(TestUserCreatorCommand).AssemblyQualifiedName!);
            Add(nameof(TestOutboxCommand), typeof(TestOutboxCommand).AssemblyQualifiedName!);
            Add(nameof(TestOutboxCommand2), typeof(TestOutboxCommand2).AssemblyQualifiedName!);

            Add(nameof(TestErrorOutboxCommand), typeof(TestErrorOutboxCommand).AssemblyQualifiedName!);
            Add(nameof(TestDelayOutboxCommand), typeof(TestDelayOutboxCommand).AssemblyQualifiedName!);
        }
    }
}