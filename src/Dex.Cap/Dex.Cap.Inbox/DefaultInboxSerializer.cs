using System;
using System.Text.Json;
using Dex.Cap.Inbox.Interfaces;

namespace Dex.Cap.Inbox;

internal sealed class DefaultInboxSerializer : IInboxSerializer
{
    private readonly JsonSerializerOptions _options = new();

    public string Serialize(Type type, object obj)
    {
        return JsonSerializer.Serialize(obj, type, _options);
    }

    public object? Deserialize(Type type, string input)
    {
        return JsonSerializer.Deserialize(input, type, _options);
    }
}
