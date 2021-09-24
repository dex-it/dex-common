using System.Diagnostics;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;

namespace Dex.MassTransit.ActivityTrace
{
    internal class ActivityTracingConsumeFilter : IFilter<ConsumeContext>
    {
        public void Probe(ProbeContext context)
        {
        }

        public async Task Send(ConsumeContext context, IPipe<ConsumeContext> next)
        {
            var operationName = $"Consuming message: {context.DestinationAddress.GetExchangeName()}";

            using (var activity = new Activity(operationName))
            {
                var parentId = context.Headers.Get<string>("MT-Activity-Id");
                if (parentId != null)
                    activity.SetParentId(parentId);
                activity.Start();

                activity.AddBaggage("destination-address", context.DestinationAddress?.ToString());
                activity.AddBaggage("source-address", context.SourceAddress?.ToString());
                activity.AddBaggage("initiator-id", context.InitiatorId?.ToString());

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