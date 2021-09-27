using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.MassTransit.Sample.Domain;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace Dex.MassTransit.Sample.Publisher
{
    internal class HelloMessageGeneratorHostedService : IHostedService
    {
        private readonly ISendEndpointProvider _sendEndpoint;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly CancellationTokenSource _tokenSource = new();

        public HelloMessageGeneratorHostedService(ISendEndpointProvider sendEndpoint, IPublishEndpoint publishEndpoint)
        {
            _sendEndpoint = sendEndpoint ?? throw new ArgumentNullException(nameof(sendEndpoint));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            while (!_tokenSource.Token.IsCancellationRequested)
            {
                // await _sendEndpoint.Send(new HelloMessage() {Hi = "Send, Hi wo, " + DateTime.UtcNow.ToString("T")}, cancellationToken);
                await _publishEndpoint.Publish(new HelloMessageDto() {Hi = "Publish, Hi wo, " + DateTime.UtcNow.ToString("T")}, cancellationToken);
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