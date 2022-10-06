using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.MassTransit.Sample.Domain;
using Dex.MassTransit.Sample.Domain.Bus;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace Dex.MassTransit.Sample.Publisher
{
    internal class OtherBusGeneratorHostedService : IHostedService
    {
        private readonly IOtherRabbitMqBus _otherRabbitMqBus;
        private readonly CancellationTokenSource _tokenSource = new();

        public OtherBusGeneratorHostedService(IOtherRabbitMqBus otherRabbitMqBus)
        {
            _otherRabbitMqBus = otherRabbitMqBus ?? throw new ArgumentNullException(nameof(otherRabbitMqBus));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            while (!_tokenSource.Token.IsCancellationRequested)
            {
                // ISendEndpointProvider not works here
                await _otherRabbitMqBus.Send(new OtherMessageDto
                {
                    Hi = "Send Hello other message",
                    TestUri = new Uri("https://masstransit-project.com/usage/producers.html#send"),
                    Date = DateTime.UtcNow
                }, cancellationToken);

                await Task.Delay(1000, cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _tokenSource.Cancel();
            return Task.CompletedTask;
        }
    }
}