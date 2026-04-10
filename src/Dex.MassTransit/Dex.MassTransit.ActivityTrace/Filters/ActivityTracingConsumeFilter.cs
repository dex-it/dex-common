using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MassTransit;

namespace Dex.MassTransit.ActivityTrace.Filters
{
    internal class ActivityTracingConsumeFilter : IFilter<ConsumeContext>
    {
        public void Probe(ProbeContext context)
        {
        }

        public async Task Send(ConsumeContext context, IPipe<ConsumeContext> next)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(next);

            var operationName = $"Consuming message: {context.DestinationAddress?.GetExchangeName()}";

            using (var activity = new Activity(operationName))
            {
                var parentId = context.Headers.Get<string>(Consts.ActivityIdName);
                if (parentId != null)
                {
                    activity.SetParentId(parentId);
                }

                activity.Start();
                try
                {
                    await next.Send(context).ConfigureAwait(false);
                }
                finally
                {
                    activity.Stop();
                }
            }
        }
    }
}