using MassTransit;

namespace Dex.MassTransit.Test
{
    public class HelloMessage : IConsumer
    {
        public string Hi { get; set; }
    }
}