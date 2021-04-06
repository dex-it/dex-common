using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace Dex.MassTransit.Sample.Test
{
    internal class HelloMessageGenerator : IHostedService
    {
        private readonly ISendEndpointProvider _sendEndpoint;
        private readonly CancellationTokenSource _tokenSource = new();

        public HelloMessageGenerator(ISendEndpointProvider sendEndpoint)
        {
            _sendEndpoint = sendEndpoint ?? throw new ArgumentNullException(nameof(sendEndpoint));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            while (!_tokenSource.Token.IsCancellationRequested)
            {
                await _sendEndpoint.Send(new HelloMessage() {Hi = "Hi wo, " + DateTime.UtcNow.ToString("T")}, cancellationToken);
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