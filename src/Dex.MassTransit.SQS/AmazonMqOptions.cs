namespace Dex.MassTransit.Extensions.Options
{
    public class AmazonMqOptions
    {
        public string Region { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string OwnerId { get; set; }
    }
}