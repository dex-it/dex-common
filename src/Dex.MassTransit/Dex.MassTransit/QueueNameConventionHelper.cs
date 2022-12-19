using System;
using System.Linq;
using System.Reflection;
using Dex.Extensions;

namespace Dex.MassTransit
{
    public static class QueueNameConventionHelper
    {
        public static string GetOnlyQueueName<TMessage>() where TMessage : class
        {
            return typeof(TMessage).Name.ReplaceRegex("(?i)dto(?-i)$", "");
        }

        public static string GetOnlyQueueName(this Uri uri)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));

            return uri.Segments.Last();
        }

        public static string GetName(this Uri uri, Type consumerType, string? serviceName)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (consumerType == null) throw new ArgumentNullException(nameof(consumerType));

            serviceName = string.IsNullOrWhiteSpace(serviceName)
                ? Assembly.GetEntryAssembly()?.GetName().Name
                : serviceName;

            return serviceName + "_" + GetOnlyQueueName(uri) + "_" + consumerType.Name.Replace("`", string.Empty);
        }
    }
}