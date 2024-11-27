using System;
using System.Collections.Generic;

namespace Dex.Cap.Outbox.Interfaces;

public interface IOutboxTypeDiscriminator
{
    IReadOnlyDictionary<string, Type> Discriminators { get; }
    
    string ResolveDiscriminator(Type type);
    Type ResolveType(string discriminator);
}