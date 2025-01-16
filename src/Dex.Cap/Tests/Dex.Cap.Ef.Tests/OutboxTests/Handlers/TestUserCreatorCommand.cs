using System;
using Dex.Cap.Common.Interfaces;

namespace Dex.Cap.Ef.Tests.OutboxTests.Handlers
{
    public class TestUserCreatorCommand : IOutboxMessage
    {
        public Guid Id { get; set; }
        public string? UserName { get; set; }
        public Guid MessageId { get; init; } = Guid.NewGuid();
    }
}