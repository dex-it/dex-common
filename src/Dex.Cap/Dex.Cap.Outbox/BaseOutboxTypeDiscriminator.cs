using System;
using System.Collections.Frozen;
using System.Linq;
using Dex.Cap.Outbox.Exceptions;
using Dex.Cap.Outbox.Interfaces;
using Dex.Types;

namespace Dex.Cap.Outbox;

public abstract class BaseOutboxTypeDiscriminator : IOutboxTypeDiscriminator
{
    private UniqueValueDictionary<string, Type> Discriminator { get; } = new();

    private FrozenDictionary<string, Type>? _discriminatorFrozenCache;
    private FrozenDictionary<Type, string>? _discriminatorInvertedFrozenCache;

    public string[] GetDiscriminators()
    {
        return Discriminator.Keys.ToArray();
    }

    protected void Add(string discriminator, Type value)
    {
        InvalidateFrozenCache();
        Discriminator.Add(discriminator, value);
    }

    protected void Add<T>(string discriminator)
    {
        InvalidateFrozenCache();
        Add(discriminator, typeof(T));
    }

    public string ResolveDiscriminator(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        _discriminatorInvertedFrozenCache ??= Discriminator.ToFrozenDictionary(x => x.Value, y => y.Key);

        if (_discriminatorInvertedFrozenCache.TryGetValue(type, out var discriminator) is false)
            throw new DiscriminatorResolveException($"Can't find discriminator for Type - {type.FullName}.");

        return discriminator;
    }

    public Type ResolveType(string discriminator)
    {
        ArgumentNullException.ThrowIfNull(discriminator);

        _discriminatorFrozenCache ??= Discriminator.ToFrozenDictionary();

        if (_discriminatorFrozenCache.TryGetValue(discriminator, out var type) is false)
            throw new DiscriminatorResolveTypeException($"Can't find Type for discriminator - {discriminator}.");

        return type;
    }

    private void InvalidateFrozenCache()
    {
        _discriminatorFrozenCache = null;
        _discriminatorInvertedFrozenCache = null;
    }
}