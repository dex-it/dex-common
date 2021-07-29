namespace Dex.MassTransit.Rabbit
{
    public class RabbitMqOptions
    {
        public bool IsSecure { get; set; }
        public string Host { get; set; } = "localhost";
        public ushort Port { get; set; } = 5672;
        public string VHost { get; set; } = "/";
        public string Username { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string? CertificatePath { get; set; }

        public override string ToString()
        {
            var schema = IsSecure ? "amqps" : "amqp";
            return $"{schema}://{Username}:{Password}@{Host}:{Port}{VHost}";
        }
    }
}