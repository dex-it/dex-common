using System;
using System.Linq;
using System.Threading.Tasks;
using Dex.MassTransit.Sample.Domain;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Dex.MassTransit.Sample.Consumer.Consumers
{
    public class HelloConsumer2 : IConsumer<HelloMessageDto>
    {
        private readonly ILogger<HelloConsumer2> _logger;

        public HelloConsumer2(ILogger<HelloConsumer2> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<HelloMessageDto> context)
        {
            _logger.LogInformation($"{context.Message.Hi} Uri: {context.Message.TestUri} SingleDevice = {context.Message.SingleDevice.ToString()} " +
                                   $"Devices = {string.Join(',', context.Message.Devices.Select(d => d.DeviceToken + d.MobilePlatform))}");

            return Task.CompletedTask;
        }
    }
}