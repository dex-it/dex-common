using System;

namespace Dex.Cap.Outbox.Interfaces
{
    public interface IOutboxSerializer
    {
        string Serialize<T>(T message);
        string Serialize<T>(T message, Type type);
        T? Deserialize<T>(string message);
        object? Deserialize(Type type, string message);
    }
}