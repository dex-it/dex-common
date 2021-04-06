using Newtonsoft.Json;

namespace Dex.MassTransit.Extensions.Options
{
    public class RabbitMqOptions
    {
        public string Host { get; set; } = "localhost";
        public ushort Port { get; set; } = 5672;
        public string VHost { get; set; } = "/";
        
        public string Username { get; set; } = "guest";
        public string Password { get; set; } = "guest";
    }
}