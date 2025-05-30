﻿using System;
using System.Linq;
using Dex.Cap.Outbox.Exceptions;
using Dex.Cap.Outbox.Interfaces;
using Dex.Types;

namespace Dex.Cap.Outbox;

public abstract class BaseOutboxTypeDiscriminator : IOutboxTypeDiscriminator
{
    private UniqueValueDictionary<string, Type> Discriminator { get; } = new();

    public string[] GetDiscriminators()
    {
        return Discriminator.Keys.ToArray();
    }

    protected void Add(string discriminator, Type value)
    {
        Discriminator.Add(discriminator, value);
    }

    protected void Add<T>(string discriminator)
    {
        Add(discriminator, typeof(T));
    }

    public string ResolveDiscriminator(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!Discriminator.TryGetKey(type, out var discriminator))
        {
            throw new DiscriminatorResolveException($"Can't find discriminator for Type - {type.FullName}.");
        }

        return discriminator;
    }

    public Type ResolveType(string discriminator)
    {
        ArgumentNullException.ThrowIfNull(discriminator);

        if (!Discriminator.TryGetValue(discriminator, out var type))
        {
            throw new DiscriminatorResolveTypeException($"Can't find Type for discriminator - {discriminator}.");
        }

        return type;
    }
}