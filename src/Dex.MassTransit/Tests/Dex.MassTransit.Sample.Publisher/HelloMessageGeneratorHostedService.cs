using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dex.MassTransit.Sample.Domain;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dex.MassTransit.Sample.Publisher
{
    internal class HelloMessageGeneratorHostedService : IHostedService
    {
        private readonly ISendEndpointProvider _sendEndpoint;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<HelloMessageGeneratorHostedService> _logger;
        private readonly CancellationTokenSource _tokenSource = new();

        public HelloMessageGeneratorHostedService(ISendEndpointProvider sendEndpoint, IPublishEndpoint publishEndpoint,
            ILogger<HelloMessageGeneratorHostedService> logger)
        {
            _sendEndpoint = sendEndpoint ?? throw new ArgumentNullException(nameof(sendEndpoint));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            while (!_tokenSource.Token.IsCancellationRequested)
            {
                //await _sendEndpoint.Send(new HelloMessageDto() {Hi = "Send, Hi wo, " + DateTime.UtcNow.ToString("T")}, cancellationToken);
                var publishHiThere = "Publish, Hi there, " + DateTime.UtcNow.ToString("T");

                var act = new Activity("Publish Test Messqge");
                act.Start();

                await _sendEndpoint.Send(
                    new HelloMessageDto
                    {
                        Hi = publishHiThere,
                        TestUri = new Uri($"test/url/{Guid.NewGuid()}", UriKind.RelativeOrAbsolute),
                        Devices = new[]
                        {
                            new MobileDevice(MobilePlatform.Android, Guid.NewGuid().ToString()),
                            new MobileDevice(MobilePlatform.IOS, Guid.NewGuid().ToString())
                        },
                        SingleDevice = new MobileDevice(MobilePlatform.Huawei, "huawei")
                    }, cancellationToken);

                _logger.LogInformation("Published - {Msg}", publishHiThere);
                act.Stop();

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