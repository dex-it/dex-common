using System;
using Dex.Cap.Outbox.Options;

namespace Dex.Cap.Outbox.Exceptions;

/// <summary>
/// Бросается на постановке, когда размер сериализованного тела превышает
/// <see cref="OutboxOptions.MaxContentLengthBytes"/>.
/// </summary>
/// <remarks>
/// Ошибка возникает на пути постановки в аутбокс, до записи строки в БД, поэтому у вызывающего есть тип
/// сообщения и размеры в контексте, а не отказ на уровне БД при вставке.
/// </remarks>
public class OutboxContentTooLargeException : OutboxException
{
    /// <summary>Стандартный конструктор без деталей.</summary>
    /// <remarks>
    /// Библиотека им не пользуется: она всегда бросает конструктором с деталями. Здесь и у двух следующих
    /// конструкторов <see cref="MessageType"/> остаётся пустым, а <see cref="ContentLengthBytes"/> и
    /// <see cref="MaxContentLengthBytes"/> нулями, поэтому лог вида <c>{MessageType} {ContentLengthBytes}</c> напечатает
    /// пустоту и нули.
    /// </remarks>
    public OutboxContentTooLargeException()
    {
    }

    /// <summary>Стандартный конструктор с сообщением, без деталей.</summary>
    /// <param name="message">Текст ошибки.</param>
    public OutboxContentTooLargeException(string message) : base(message)
    {
    }

    /// <summary>Стандартный конструктор с сообщением и вложенным исключением, без деталей.</summary>
    /// <param name="message">Текст ошибки.</param>
    /// <param name="innerException">Исключение-причина.</param>
    public OutboxContentTooLargeException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>Конструктор, которым бросает сама библиотека: заполняет все детали отказа.</summary>
    /// <param name="messageType">Дискриминатор типа сообщения.</param>
    /// <param name="contentLengthBytes">Фактический размер тела в байтах UTF-8.</param>
    /// <param name="maxContentLengthBytes">Настроенный предел в байтах UTF-8.</param>
    public OutboxContentTooLargeException(string messageType, int contentLengthBytes, int maxContentLengthBytes)
        : base(
            $"Outbox message '{messageType}' body is {contentLengthBytes} bytes, which exceeds the configured " +
            $"{nameof(OutboxOptions)}.{nameof(OutboxOptions.MaxContentLengthBytes)} of {maxContentLengthBytes}.")
    {
        MessageType = messageType;
        ContentLengthBytes = contentLengthBytes;
        MaxContentLengthBytes = maxContentLengthBytes;
    }

    /// <summary>Дискриминатор сообщения, тело которого превысило лимит.</summary>
    public string MessageType { get; } = string.Empty;

    /// <summary>Фактический размер тела в байтах (UTF-8).</summary>
    public int ContentLengthBytes { get; }

    /// <summary>Настроенный предел в байтах (UTF-8).</summary>
    public int MaxContentLengthBytes { get; }
}