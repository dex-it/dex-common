using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Dex.MassTransit.Rabbit;

public abstract class BaseConsumer<TMessage>(ILogger logger) : IConsumer<TMessage> where TMessage : class
{
    private ConsumeContext<TMessage>? _context;
    protected ILogger Logger { get; } = logger;

    public virtual async Task Consume(ConsumeContext<TMessage> context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

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

    /// <summary>
    /// Предельный размер тела сообщения в логе, в байтах UTF-8.
    /// </summary>
    protected virtual int MessageDataLimit => ConsumerLoggerExtensions.DefaultMessageDataLimit;

    /// <summary>
    /// Запись об упавшей обработке сообщения.
    /// </summary>
    protected virtual void LogError(ConsumeContext<TMessage> context, Exception e)
        => Logger.LogConsumeError(context, e, MessageDataLimit);

    private sealed class DeferConsumerException : Exception;
}