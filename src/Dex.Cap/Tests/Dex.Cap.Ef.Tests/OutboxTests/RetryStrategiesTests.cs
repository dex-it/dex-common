using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.OutboxTests.RetryStrategies;
using Dex.Cap.Outbox.Extensions;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;
using Dex.Cap.Outbox.RetryStrategies;
using Dex.Extensions;
using Dex.Outbox.Command.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests
{
    public class RetryStrategiesTests : BaseTest
    {
        [Test]
        [TestCase(100, 1, OutboxMessageStatus.Succeeded, "Incremental")]
        [TestCase(1000, 0, OutboxMessageStatus.Failed, "Incremental")]
        [TestCase(100, 1, OutboxMessageStatus.Succeeded, "Exponential")]
        [TestCase(1000, 0, OutboxMessageStatus.Failed, "Exponential")]
        public async Task IncrementalRetry_ProcessMessage(int intervalMs, int expectedCount, OutboxMessageStatus expectedStatus, string retryStrategyName)
        {
            var serviceProvider = InitServiceCollection(strategyConfigure: strategyConfigurator =>
                    ConfigureStrategy(intervalMs, retryStrategyName, strategyConfigurator))
                .AddScoped<IOutboxMessageHandler<TestErrorOutboxCommand>, TestErrorCommandHandler>()
                .BuildServiceProvider();

            TestErrorCommandHandler.Reset();

            var outboxService = serviceProvider.GetRequiredService<IOutboxService<TestDbContext>>();
            var correlationId = Guid.NewGuid();
            await outboxService.EnqueueAsync(correlationId, new TestErrorOutboxCommand { MaxCount = 2 });
            await SaveChanges(serviceProvider);

            var count = 0;
            TestErrorCommandHandler.OnProcess += (_, _) => { count++; };
            var handler = serviceProvider.GetRequiredService<IOutboxHandler>();

            var repeat = 5;
            while (repeat-- > 0)
            {
                await handler.ProcessAsync(CancellationToken.None);
                await Task.Delay(100);
            }

            var envelope = await GetDb(serviceProvider).Set<OutboxEnvelope>().FirstAsync(x => x.CorrelationId == correlationId);
            Assert.AreEqual(expectedStatus, envelope.Status);
            Assert.AreEqual(expectedCount, count);
        }

        private static void ConfigureStrategy(int interval, string retryStrategyName, OutboxRetryStrategyConfigurator configurator)
        {
            switch (retryStrategyName)
            {
                case "Incremental":
                    configurator.UseOutboxIncrementalRetryStrategy(interval.MilliSeconds());
                    break;
                case "Exponential":
                    configurator.UseOutboxExponentialRetryStrategy(interval.MilliSeconds());
                    break;
            }
        }
    }
}