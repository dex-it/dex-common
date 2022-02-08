using System.Collections.Generic;
using System.Linq;
using MassTransit;
using MassTransit.Configuration;

namespace Dex.MassTransit.ActivityTrace
{
    internal class ActivityTracingPipeSpecification : IPipeSpecification<ConsumeContext>
    {
        public void Apply(IPipeBuilder<ConsumeContext> builder)
        {
            builder.AddFilter(new ActivityTracingConsumeFilter());
        }

        public IEnumerable<ValidationResult> Validate()
        {
            return Enumerable.Empty<ValidationResult>();
        }
    }
}