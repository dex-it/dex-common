using System;
using MassTransit;

namespace Dex.MassTransit.ActivityTrace
{
    /// <summary>
    /// Add midleware to enable Activity tracing propagation for all consumers.
    /// </summary>
    public static class ActivityMiddlewareConfiguratorExtensions
    {
        /// <summary>
        /// Add ActivityTracing specification for IPipeSpecification[ConsumeContext].
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void LinkActivityTracingContext(this IBusFactoryConfigurator value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            var activityTracingPipeSpecification = new ActivityTracingPipeSpecification();
            value.AddPipeSpecification(activityTracingPipeSpecification);
            value.ConfigureSend(configurator => configurator.AddPipeSpecification(activityTracingPipeSpecification));
            value.ConfigurePublish(configurator => configurator.AddPipeSpecification(activityTracingPipeSpecification));
        }
    }
}