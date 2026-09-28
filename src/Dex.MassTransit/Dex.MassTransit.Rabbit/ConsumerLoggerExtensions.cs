using System;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Dex.MassTransit.Rabbit;

/// <summary>
/// Запись в лог об упавшей обработке сообщения.
/// </summary>
public static class ConsumerLoggerExtensions
{
    /// <summary>
    /// Предельный размер тела сообщения в логе по умолчанию, в байтах UTF-8.
    /// </summary>
    /// <remarks>
    /// Размер тела ничем не ограничен сверху, а хранилище логов держит значение в одном поле
    /// с ограничением на длину: без потолка запись либо не попадает в индекс целиком, либо
    /// вытесняет из него остальную диагностику.
    /// </remarks>
    public const int DefaultMessageDataLimit = 4000;

    /// <summary>
    /// Пишет запись об упавшей обработке: тип сообщения, идентификаторы, номер попытки и усечённое тело.
    /// </summary>
    /// <remarks>
    /// Вынесено из <see cref="BaseConsumer{TMessage}"/>: консьюмер на несколько типов сообщений
    /// базовый класс наследовать не может, а запись об ошибке ему нужна такая же, поэтому он зовёт
    /// это из своего catch. Тело маскирует <see cref="IMessageDataMasker"/> из контейнера консьюмера, если он зарегистрирован.
    /// </remarks>
    public static void LogConsumeError<TMessage>(this ILogger logger, ConsumeContext<TMessage> context, Exception e,
        int messageDataLimit = DefaultMessageDataLimit)
        where TMessage : class
    {
        logger.LogError(e,
            "Consumer process failed. MessageType: {MessageType}, MessageId: {MessageId}, ConversationId: {ConversationId}, RetryAttempt: {RetryAttempt}, MessageData: {MessageData}",
            typeof(TMessage).FullName, context.MessageId, context.ConversationId, context.GetRetryAttempt(),
            FormatMessageData(context, messageDataLimit));
    }

    /// <remarks>
    /// Контейнер MassTransit кладёт в контекст консьюмера, созданного через DI. Сбой разрешения
    /// оставляет неизвестным, подключён ли маскировщик, поэтому тело в таком случае не пишется.
    /// </remarks>
    private static string FormatMessageData<TMessage>(ConsumeContext<TMessage> context, int limit)
        where TMessage : class
    {
        IMessageDataMasker? masker;

        try
        {
            masker = context.TryGetPayload<IServiceProvider>(out var services)
                ? services.GetService(typeof(IMessageDataMasker)) as IMessageDataMasker
                : null;
        }
        catch (Exception e)
        {
            return MessageDataFormatter.NotMasked(e);
        }

        return MessageDataFormatter.Format(context.Message, limit, masker);
    }
}