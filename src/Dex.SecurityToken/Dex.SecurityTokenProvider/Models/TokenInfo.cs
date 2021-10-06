using System;

namespace Dex.SecurityTokenProvider.Models
{
    public record TokenInfo 
    {
        public Guid Id { get; set; }
        public DateTimeOffset Expired { get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public bool Activated { get; set; }
    }
}