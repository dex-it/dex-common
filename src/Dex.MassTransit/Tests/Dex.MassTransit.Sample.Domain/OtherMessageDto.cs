using System;
using MassTransit;

namespace Dex.MassTransit.Sample.Domain
{
    public class OtherMessageDto : IConsumer
    {
        public string? Hi { get; set; }
        
        public DateTime Date { get; set; }
        
        public Uri? TestUri { get; set; }
    }
}