using System;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Dex.SecurityTokenProvider.Models
{
    public abstract class BaseToken
    {
        public Guid Id { get; set; }
        
        /// <summary>
        /// //Имя реусрса который выдал токен
        /// Использовать токен можно только в том ресурсе, который его выдал
        /// </summary>
        public string? Audience { get; set; }
        /// <summary>
        ///  - причина (произвольная строка)
        /// </summary>
        public string? Reason  { get; set; }
        /// <summary>
        /// - когда выдан
        /// </summary>
        public DateTimeOffset Created   { get; init; }
        /// <summary>
        /// -  истечет
        /// </summary>
        public DateTimeOffset Expired    { get; set; }
    }
}