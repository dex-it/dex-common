using System;
using MassTransit;

namespace Dex.MassTransit.Sample.Domain
{
    public class HelloMessageDto : IConsumer
    {
        public string Hi { get; set; }
        
        public Uri TestUri { get; set; }
        
        public MobileDevice[]? Devices {get; set; }
        
        // for example
        public MobileDevice? SingleDevice { get; set; }
    }
}