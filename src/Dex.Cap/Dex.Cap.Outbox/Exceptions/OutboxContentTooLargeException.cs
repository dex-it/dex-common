using System;
using Dex.Cap.Outbox.Options;

namespace Dex.Cap.Outbox.Exceptions;

/// <summary>
/// Бросается на постановке, когда размер сериализованного тела превышает
/// <see cref="OutboxOptions.MaxContentLength"/>.
/// </summary>
/// <remarks>
/// Ошибка возникает на пути постановки в аутбокс, до записи строки в БД, поэтому у вызывающего есть тип
/// сообщения и размеры в контексте, а не отказ на уровне БД при вставке.
/// </remarks>
public class OutboxContentTooLargeException : OutboxException
{
    public OutboxContentTooLargeException()
    {
    }

    public OutboxContentTooLargeException(string message) : base(message)
    {
    }

    public OutboxContentTooLargeException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public OutboxContentTooLargeException(string messageType, int contentLength, int maxContentLength)
        : base(
            $"Outbox message '{messageType}' body is {contentLength} bytes, which exceeds the configured " +
            $"{nameof(OutboxOptions.MaxContentLength)} of {maxContentLength} bytes.")
    {
        MessageType = messageType;
        ContentLength = contentLength;
        MaxContentLength = maxContentLength;
    }

    /// <summary>Дискриминатор сообщения, тело которого превысило лимит.</summary>
    public string MessageType { get; } = string.Empty;

    /// <summary>Фактический размер тела в байтах (UTF-8).</summary>
    public int ContentLength { get; }

    /// <summary>Настроенный предел в байтах (UTF-8).</summary>
    public int MaxContentLength { get; }
}