using Dex.Cap.Outbox.Interfaces;
using Dex.Types;

namespace Dex.Cap.Outbox;

public abstract class BaseOutboxTypeDiscriminator : IOutboxTypeDiscriminator
{
    private UniqueValueDictionary<string, string> Discriminator { get; } = new();

    protected void Add(string key, string value)
    {
        Discriminator.Add(key, value);
    }

    public bool TryGetDiscriminator(string type, out string discriminator)
    {
        return Discriminator.TryGetKey(type, out discriminator);
    }

    public bool TryGetType(string discriminator, out string type)
    {
        return Discriminator.TryGetValue(discriminator, out type);
    }
}