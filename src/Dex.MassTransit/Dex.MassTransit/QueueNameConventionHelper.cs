using System;
using System.Linq;
using System.Reflection;

namespace Dex.MassTransit
{
    public static class QueueNameConventionHelper
    {
        public static string GetName(this Uri uri, Type consumerType, string? serviceName)
        {
            serviceName = string.IsNullOrWhiteSpace(serviceName)
                ? Assembly.GetEntryAssembly()?.GetName().Name
                : serviceName;

            return serviceName + "_" + GetName(uri) + "_" + consumerType.Name.Replace("`", string.Empty);
        }

        public static string GetName(this Uri uri)
        {
            return uri.Segments.Last();
        }
    }
}