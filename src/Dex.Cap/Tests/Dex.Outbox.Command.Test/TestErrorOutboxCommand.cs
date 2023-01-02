using System;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;

namespace Dex.Outbox.Command.Test
{
    public class TestErrorOutboxCommand : BaseOutboxMessage
    {
        public int MaxCount { get; set; } = 1;
    }
}