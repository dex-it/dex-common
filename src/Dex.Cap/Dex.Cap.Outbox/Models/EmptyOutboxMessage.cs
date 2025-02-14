using System;
using System.Text.Json.Serialization;
using Dex.Cap.Common.Interfaces;

namespace Dex.Cap.Outbox.Models
{
    internal sealed class EmptyOutboxMessage : IOutboxMessage
    {
        public static readonly EmptyOutboxMessage Empty = new();

        [JsonIgnore]
        public Guid MessageId { get; init; } = Guid.Empty;
    }
}