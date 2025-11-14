using System;
using Dex.Cap.Common.Interfaces;

namespace Dex.Cap.Ef.Tests.OutboxTests.Handlers;

public class TestUserCreatorCommand : IIdempotentKey, IOutboxMessage
{
    public static string OutboxTypeId => "6262F3D6-498F-4820-B372-6C3425824CD9";

    public Guid Id { get; init; }

    public string? UserName { get; init; }

    public string IdempotentKey { get; init; } = Guid.NewGuid().ToString("N");
}