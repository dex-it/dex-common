using System;
using System.Threading.Tasks;
using Dex.MassTransit.Sample.Domain;
using MassTransit;

namespace Dex.MassTransit.Sample.Consumer.Consumers
{
    public class OtherConsumer : IConsumer<OtherMessageDto>
    {
        public Task Consume(ConsumeContext<OtherMessageDto> context)
        {
            Console.WriteLine($"{context.Message.Hi} - {context.Message.Date}. Url: {context.Message.TestUri}");
            return Task.CompletedTask;
        }
    }
}