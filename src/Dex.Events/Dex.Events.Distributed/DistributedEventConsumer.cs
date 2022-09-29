using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Dex.Events.Distributed
{
    /// <summary>
    /// Template consumer
    /// </summary>
    /// <typeparam name="T">DistributedBaseEventParams</typeparam>
    public class DistributedEventConsumer<T> : IConsumer<T>
        where T : DistributedBaseEventParams
    {
        private readonly IEnumerable<IDistributedEventHandler<T>> _handlers;
        private readonly ILogger<DistributedEventConsumer<T>> _logger;

        public DistributedEventConsumer(IEnumerable<IDistributedEventHandler<T>> handlers, ILogger<DistributedEventConsumer<T>> logger)
        {
            _handlers = handlers;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<T> context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            foreach (var eventHandler in _handlers)
            {
                try
                {
                    await eventHandler.ProcessAsync(context.Message, context.CancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError("{Type}. Exception: {Exception}", eventHandler.GetType(), ex.Message);
                    throw;
                }
            }
        }
    }
}