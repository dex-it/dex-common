using System;
using System.Collections.Generic;

namespace Dex.Cap.Outbox.Interfaces;

public interface IOutboxTypeDiscriminator
{
    IReadOnlyCollection<string> GetDiscriminators();
    string ResolveDiscriminator(Type type);
    Type ResolveType(string discriminator);
}