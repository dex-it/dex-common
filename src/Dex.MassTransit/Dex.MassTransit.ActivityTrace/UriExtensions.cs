using System;

namespace Dex.MassTransit.ActivityTrace
{
    internal static class UriExtensions
    {
        public static string GetExchangeName(this Uri value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            
            var exchange = value.LocalPath;
            var messageType = exchange[(exchange.LastIndexOf('/') + 1)..];
            return messageType ?? string.Empty;
        }
    }
}