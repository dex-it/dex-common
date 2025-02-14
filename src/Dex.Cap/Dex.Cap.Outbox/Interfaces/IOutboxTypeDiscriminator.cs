using System;

namespace Dex.Cap.Outbox.Interfaces;

public interface IOutboxTypeDiscriminator
{
    string[] GetDiscriminators();
    string ResolveDiscriminator(Type type);
    Type ResolveType(string discriminator);
}