using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.OutboxTests.RetryStrategies;
using Dex.Cap.Outbox.Ef.Extensions;
using Dex.Cap.Outbox.Extensions;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;
using Dex.Cap.Outbox.Options;
using Dex.Extensions;
using Dex.Outbox.Command.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests
{
    public class RetryStrategiesTests
    {
        [Test]
        [TestCase(100, 1, OutboxMessageStatus.Succeeded, "Incremental")]
        [TestCase(1000, 0, OutboxMessageStatus.Failed, "Incremental")]
        [TestCase(100, 1, OutboxMessageStatus.Succeeded, "Exponential")]
        [TestCase(1000, 0, OutboxMessageStatus.Failed, "Exponential")]
        public async Task IncrementalRetry_ProcessMessage(int intervalMilliseconds, int expectedCount, OutboxMessageStatus expectedStatus, string retryStrategy)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder =>
            {
                builder.AddDebug();
                builder.AddProvider(new TestLoggerProvider());
                builder.SetMinimumLevel(LogLevel.Error);
            });
            serviceCollection
                .AddScoped<IOutboxMessageHandler<TestErrorOutboxCommand>, TestErrorCommandHandler>()
                .AddScoped(_ => new TestDbContext("db_test_" + Guid.NewGuid().ToString("N")))
                .AddOutbox<TestDbContext, TestDiscriminator>((_, configurator) =>
                {
                    switch (retryStrategy)
                    {
                        case "Incremental":
                            configurator.UseOutboxIncrementalRetryStrategy(intervalMilliseconds.MilliSeconds());
                            break;
                        case "Exponential":
                            configurator.UseOutboxExponentialRetryStrategy(intervalMilliseconds.MilliSeconds());
                            break;
                    }
                })
                .AddOptions<OutboxOptions>();
            serviceCollection.AddSingleton<IOutboxTypeDiscriminator, TestDiscriminator>();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            TestErrorCommandHandler.Reset();

            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();
            await dbContext.Database.EnsureCreatedAsync();

            var outboxService = serviceProvider.GetRequiredService<IOutboxService<TestDbContext>>();
            var correlationId = Guid.NewGuid();
            await outboxService.EnqueueAsync(correlationId, new TestErrorOutboxCommand { MaxCount = 2 });
            await dbContext.SaveChangesAsync();

            var count = 0;
            TestErrorCommandHandler.OnProcess += (_, _) => { count++; };
            var handler = serviceProvider.GetRequiredService<IOutboxHandler>();

            var repeat = 5;
            while (repeat-- > 0)
            {
                await handler.ProcessAsync(CancellationToken.None);
                await Task.Delay(100);
            }

            var envelope = await dbContext.Set<OutboxEnvelope>().FirstAsync(x => x.CorrelationId == correlationId);
            Assert.AreEqual(expectedStatus, envelope.Status);
            Assert.AreEqual(expectedCount, count);

            await dbContext.Database.EnsureDeletedAsync();
        }
    }
}