using System;
using Dex.Cap.Common.Interfaces;

namespace Dex.Cap.Ef.Tests.OutboxTests.Handlers
{
    public class TestUserCreatorCommand : IIdempotentKey
    {
        public Guid Id { get; set; }
        public string? UserName { get; set; }
        public string IdempotentKey { get; init; } = Guid.NewGuid().ToString("N");
    }
}