using System;
using Dex.Cap.Inbox.Options;

namespace Dex.Cap.Inbox.Exceptions;

/// <summary>
/// Бросается на приёме, когда размер сериализованного тела превышает
/// <see cref="InboxOptions.MaxContentLength"/>.
/// </summary>
/// <remarks>
/// Ошибка возникает на пути приёма, до записи строки в БД, поэтому у вызывающего есть тип сообщения и
/// размеры в контексте, а не отказ на уровне БД при вставке. Такое сообщение источнику подтверждать нельзя.
/// </remarks>
public class InboxContentTooLargeException : InboxException
{
    public InboxContentTooLargeException()
    {
    }

    public InboxContentTooLargeException(string message) : base(message)
    {
    }

    public InboxContentTooLargeException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public InboxContentTooLargeException(string messageType, int contentLength, int maxContentLength)
        : base(
            $"Inbox message '{messageType}' body is {contentLength} bytes, which exceeds the configured " +
            $"{nameof(InboxOptions.MaxContentLength)} of {maxContentLength} bytes.")
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