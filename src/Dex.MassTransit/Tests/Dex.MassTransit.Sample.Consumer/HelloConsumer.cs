using System;
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
            Console.WriteLine(context.Message.Hi);
            return Task.CompletedTask;
        }
    }
}