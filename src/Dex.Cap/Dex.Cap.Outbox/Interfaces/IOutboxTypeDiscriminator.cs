using System;

namespace Dex.Cap.Outbox.Interfaces;

public interface IOutboxTypeDiscriminator
{
    string ResolveDiscriminator(Type type);
    Type ResolveType(string discriminator);
}