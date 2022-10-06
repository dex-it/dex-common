using System.Collections.Generic;
using System.Linq;
using Dex.MassTransit.ActivityTrace.Filters;
using MassTransit;
using MassTransit.Configuration;

namespace Dex.MassTransit.ActivityTrace
{
    internal class ActivityTracingPipeSpecification :
        IPipeSpecification<ConsumeContext>,
        IPipeSpecification<SendContext>, 
        IPipeSpecification<PublishContext>
    {
        public void Apply(IPipeBuilder<ConsumeContext> builder)
        {
            builder.AddFilter(new ActivityTracingConsumeFilter());
        }

        public void Apply(IPipeBuilder<SendContext> builder)
        {
            builder.AddFilter(new ActivityTracingSendFilter());
        }

        public void Apply(IPipeBuilder<PublishContext> builder)
        {
            builder.AddFilter(new ActivityTracingSendFilter());
        }

        public IEnumerable<ValidationResult> Validate()
        {
            return Enumerable.Empty<ValidationResult>();
        }
    }
}