using System;
using System.Text.Json;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.Outbox
{
    public class DefaultOutboxSerializer : IOutboxSerializer
    {
        private readonly JsonSerializerOptions _options;

        public DefaultOutboxSerializer()
        {
            _options = new JsonSerializerOptions();
        }

        public string Serialize<T>(T message)
        {
            return JsonSerializer.Serialize(message, _options);
        }

        public T? Deserialize<T>(string message)
        {
            return JsonSerializer.Deserialize<T>(message, _options);
        }

        public object? Deserialize(Type type, string message)
        {
            return JsonSerializer.Deserialize(message, type, _options);
        }
    }
}