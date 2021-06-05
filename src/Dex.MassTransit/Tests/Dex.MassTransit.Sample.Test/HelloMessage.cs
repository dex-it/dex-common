using MassTransit;

namespace Dex.MassTransit.Sample.Test
{
    public class HelloMessage : IConsumer
    {
        public string Hi { get; set; }
    }
}