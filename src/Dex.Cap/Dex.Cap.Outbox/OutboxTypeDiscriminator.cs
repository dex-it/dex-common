using System;

namespace Dex.Cap.Outbox;

internal class OutboxTypeDiscriminator
{
    private BiDictionary<string, string> Discriminator { get; } = new();

    public void Add(string key, string value)
    {
        Discriminator.Add(key, value);
    }

    public bool GetDiscriminator(string type, out string discriminator)
    {
        return Discriminator.TryGetKey(type, out discriminator);
    }

    public bool GetType(string discriminator, out string type)
    {
        return Discriminator.TryGetValue(discriminator, out type);
    }
}