using MassTransit;

namespace Dex.MassTransit.Sample.Domain
{
    public class HelloMessageDto : IConsumer
    {
        public string Hi { get; set; }
    }
}