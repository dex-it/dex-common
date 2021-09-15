using MassTransit;

namespace Dex.MassTransit.Sample.Test
{
    public class HelloMessageDto : IConsumer
    {
        public string Hi { get; set; }
    }
}