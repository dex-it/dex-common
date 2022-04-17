using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.MassTransit.Sample.Domain;
using Dex.MassTransit.Sample.Domain.Bus;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace Dex.MassTransit.Sample.Publisher
{
    internal class HelloMessageGeneratorHostedService : IHostedService
    {
        private readonly ISendEndpointProvider _sendEndpoint;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IOtherRabbitMqBus _otherRabbitMqBus;
        private readonly CancellationTokenSource _tokenSource = new();

        public HelloMessageGeneratorHostedService(ISendEndpointProvider sendEndpoint, IPublishEndpoint publishEndpoint, IOtherRabbitMqBus otherRabbitMqBus)
        {
            _sendEndpoint = sendEndpoint ?? throw new ArgumentNullException(nameof(sendEndpoint));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
            _otherRabbitMqBus = otherRabbitMqBus ?? throw new ArgumentNullException(nameof(otherRabbitMqBus));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            while (!_tokenSource.Token.IsCancellationRequested)
            {
                 //await _sendEndpoint.Send(new HelloMessageDto() {Hi = "Send, Hi wo, " + DateTime.UtcNow.ToString("T")}, cancellationToken);
                await _sendEndpoint.Send(
                    new HelloMessageDto
                    {
                        Hi = "Publish, Hi there, " + DateTime.UtcNow.ToString("T"), 
                        TestUri = new Uri($"test/url/{Guid.NewGuid()}", UriKind.RelativeOrAbsolute),
                        Devices = new []
                        {
                            new MobileDevice(MobilePlatform.Android, Guid.NewGuid().ToString()),
                            new MobileDevice(MobilePlatform.IOS, Guid.NewGuid().ToString())
                        },
                        SingleDevice = new MobileDevice(MobilePlatform.Huawei, "huawei")
                    }, cancellationToken);
                
                await Task.Delay(1000, cancellationToken);

                await _otherRabbitMqBus.Publish<OtherMessageDto>(new OtherMessageDto
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