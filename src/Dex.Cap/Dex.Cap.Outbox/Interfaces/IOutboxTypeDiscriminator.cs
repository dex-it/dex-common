namespace Dex.Cap.Outbox.Interfaces;

public interface IOutboxTypeDiscriminator
{
    bool TryGetDiscriminator(string type, out string discriminator);
    bool TryGetType(string discriminator, out string type);
}