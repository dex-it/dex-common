using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.OnceExecutor.MassTransit;
using Dex.Outbox.Command.Test;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxPublishTests
{
    public class OutboxPublishTests : BaseTest
    {
        [Test]
        public async Task EnqueueAndPublishMessageUsingTestHarness_SuccessTrue()
        {
            var sp = InitServiceCollection()
                .AddScoped(typeof(IOutboxMessageHandler<>), typeof(PublisherOutboxHandler<>))
                .AddMassTransitTestHarness()
                .BuildServiceProvider();

            var outboxService = sp.GetRequiredService<IOutboxService>();
            var harness = sp.GetRequiredService<ITestHarness>();
            await harness.Start();

            var command = new TestOutboxCommand { Args = "hello world" };
            await outboxService.EnqueueAsync(command);
            await SaveChanges(sp);

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            Assert.IsTrue(await harness.Published.Any<TestOutboxCommand>());
            await harness.Stop();
        }
    }
}