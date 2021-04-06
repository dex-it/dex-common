using Newtonsoft.Json;

namespace Dex.MassTransit.Extensions
{
    public class RabbitMqOptions
    {
        [JsonProperty("Host")] public string Host { get; set; } = "localhost";
        [JsonProperty("Port")] public ushort Port { get; set; } = 5672;
        [JsonProperty("VHost")] public string VHost { get; set; } = "/";
        [JsonProperty("Username")] public string Username { get; set; } = "guest";
        [JsonProperty("Password")] public string Password { get; set; } = "guest";
    }
}