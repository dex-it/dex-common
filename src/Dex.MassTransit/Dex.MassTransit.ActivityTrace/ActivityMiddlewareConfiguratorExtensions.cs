using System;
using MassTransit;
using MassTransit.Context;

namespace Dex.MassTransit.ActivityTrace
{
    /// <summary>
    /// Add midleware to enable Activity tracing propagation for all consumers
    /// </summary>
    public static class ActivityMiddlewareConfiguratorExtensions
    {
        /// <summary>
        /// Add ActivityTracing specification for IPipeSpecification[ConsumeContext]
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void LinkActivityTracingContext(this IBusFactoryConfigurator value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            value.AddPipeSpecification(new ActivityTracingPipeSpecification());
        }
    }
}