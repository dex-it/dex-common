using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Dex.MassTransit.Rabbit;

public abstract class BaseConsumer<TMessage> : IConsumer<TMessage>
    where TMessage : class
{
    private ConsumeContext<TMessage>? _context;
    protected ILogger Logger { get; }

    protected BaseConsumer(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public virtual async Task Consume(ConsumeContext<TMessage> context)
    {
        context = context ?? throw new ArgumentNullException(nameof(context));
        _context = context;

        try
        {
            await Process(context);
        }
        catch (DeferConsumerException)
        {
            // ignore
        }
        catch (Exception e)
        {
            LogError(context, e);
            throw;
        }
    }

    protected abstract Task Process(ConsumeContext<TMessage> context);

    /// <summary>
    /// Прерывает текущее исполнение путем выброса DeferConsumerException.
    /// Отправляет сообщение в delay_exchange на указанный интервал.
    /// </summary>
    /// <param name="delay"></param>
    /// <exception cref="BaseConsumer{TMessage}.DeferConsumerException"></exception>
    protected async Task Defer(TimeSpan delay)
    {
        await _context.Defer(delay);
        throw new DeferConsumerException();
    }

    protected void LogError(ConsumeContext<TMessage> context, Exception e)
    {
        Logger.LogError(e, "Consumer process failed. [{@MessageData}]", context.Message);
    }

    private sealed class DeferConsumerException : Exception
    {
    }
}