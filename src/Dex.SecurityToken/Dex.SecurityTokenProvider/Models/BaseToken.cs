
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Dex.SecurityTokenProvider.Models
{
    public abstract class BaseToken
    {
        protected BaseToken()
        {
            Id = Guid.NewGuid();
            Created = DateTimeOffset.UtcNow;
        }
        public Guid Id { get; init; }
        
        /// <summary>
        /// //Имя реусрса который выдал токен
        /// Использовать токен можно только в том ресурсе, который его выдал
        /// </summary>
        public string? Audience { get; init; }

        /// <summary>
        /// - когда выдан
        /// </summary>
        public DateTimeOffset Created   { get; init; } 
        /// <summary>
        /// -  истечет
        /// </summary>
        public DateTimeOffset Expired    { get; init; }
    }
}