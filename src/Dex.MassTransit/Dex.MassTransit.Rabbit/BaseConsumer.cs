using System;
using System.Linq;
using System.Text.Json;
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

    protected void LogError(ConsumeContext<TMessage> context, Exception e)
    {
        const int messageDataLimit = 500;

        var messageJson = JsonSerializer.Serialize(context.Message);

        var messageDataOversize = messageJson.Length > messageDataLimit;
        var messageData = messageJson.Take(messageDataLimit).Concat(messageDataOversize ? "..." : []);

        Logger.LogError(e, "Consumer process failed. [{MessageData}]", messageData);
    }

    private sealed class DeferConsumerException : Exception;
}