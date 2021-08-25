using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MassTransit;

namespace Dex.MassTransit.Sample.Test
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