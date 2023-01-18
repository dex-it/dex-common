using System;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Ef.Tests.OutboxTests.Handlers
{
    public class TestUserCreatorCommand : BaseOutboxMessage
    {
        public Guid Id { get; set; }
        public string? UserName { get; set; }
    }
}