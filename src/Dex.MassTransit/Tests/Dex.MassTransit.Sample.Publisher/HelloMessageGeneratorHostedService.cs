using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dex.MassTransit.Sample.Domain;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dex.MassTransit.Sample.Publisher
{
    internal class HelloMessageGeneratorHostedService : IHostedService
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly ILogger<HelloMessageGeneratorHostedService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly CancellationTokenSource _tokenSource = new();

        public HelloMessageGeneratorHostedService(ILogger<HelloMessageGeneratorHostedService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            while (!_tokenSource.Token.IsCancellationRequested)
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                //await _sendEndpoint.Send(new HelloMessageDto() {Hi = "Send, Hi wo, " + DateTime.UtcNow.ToString("T")}, cancellationToken);
                var publishHiThere = "Publish, Hi there, " + DateTime.UtcNow.ToString("T");

                var act = new Activity("Publish Test Messqge");
                act.Start();

                await publishEndpoint.Publish(
                    new HelloMessageDto
                    {
                        Hi = publishHiThere,
                        TestUri = new Uri($"test/url/{Guid.NewGuid()}", UriKind.RelativeOrAbsolute),
                        Devices = new[]
                        {
                            new MobileDevice(MobilePlatform.Android, Guid.NewGuid().ToString()),
                            new MobileDevice(MobilePlatform.Ios, Guid.NewGuid().ToString())
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