using System.Diagnostics;
using System.Threading.Tasks;
using MassTransit;

namespace Dex.MassTransit.ActivityTrace.Filters
{
    internal class ActivityTracingSendFilter : IFilter<SendContext>, IFilter<PublishContext>
    {
        public async Task Send(SendContext context, IPipe<SendContext> next)
        {
            SetActivityHeader(context);
            await next.Send(context).ConfigureAwait(false);
        }

        public async Task Send(PublishContext context, IPipe<PublishContext> next)
        {
            SetActivityHeader(context);
            await next.Send(context).ConfigureAwait(false);
        }

        private static void SetActivityHeader(SendContext context)
        {
            if (Activity.Current?.Id != null)
                context.Headers.Set(Consts.ActivityIdName, Activity.Current.Id);
        }

        public void Probe(ProbeContext context)
        {
        }
    }
}