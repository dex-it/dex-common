using System;
using System.Linq;
using System.Threading.Tasks;
using Dex.MassTransit.Sample.Domain;
using MassTransit;

namespace Dex.MassTransit.Sample.Consumer
{
    public class HelloConsumer : IConsumer<HelloMessageDto>
    {
        public Task Consume(ConsumeContext<HelloMessageDto> context)
        {
            Console.WriteLine(context.Message.Hi);
            return Task.CompletedTask;
        }
    }

    public class HelloConsumer2 : IConsumer<HelloMessageDto>
    {
        public Task Consume(ConsumeContext<HelloMessageDto> context)
        {
            Console.WriteLine(
                $"{context.Message.Hi} Uri: {context.Message.TestUri} SingleDevice = {context.Message.SingleDevice.ToString()} Devices = {string.Join(',', context.Message.Devices.Select(d => d.DeviceToken + d.MobilePlatform))}");
            return Task.CompletedTask;
        }
    }
}