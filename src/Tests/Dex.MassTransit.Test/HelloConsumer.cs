using System;
using System.Threading.Tasks;
using MassTransit;

namespace Dex.MassTransit.Test
{
    public class HelloConsumer : IConsumer<HelloMessage>
    {
        public Task Consume(ConsumeContext<HelloMessage> context)
        {
            Console.WriteLine(context.Message.Hi);
            return Task.CompletedTask;
        }
    }
}