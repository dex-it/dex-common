using System;

namespace Dex.Cap.Inbox.Options;

public class InboxOptions
{
    /// <summary>
    /// Количество попыток обработки сообщения. По исчерпании сообщение переводится в DeadLettered.
    /// Default: 3
    /// </summary>
    public int Retries { get; set; } = 3;

    /// <summary>
    /// Количество сообщений, захватываемых обработчиком за один цикл.
    /// Время обработки ВСЕХ выбранных сообщений отсчитывается с момента их захвата.
    /// Default: 100
    /// </summary>
    public int MessagesToProcess { get; set; } = 100;

    /// <summary>
    /// Степень параллелизма обработки. ConcurrencyLimit должен быть меньше либо равен MessagesToProcess.
    /// Default: 1
    /// </summary>
    public int ConcurrencyLimit { get; set; } = 1;

    /// <summary>
    /// Таймаут захвата партии сообщений из хранилища.
    /// Default: 20sec
    /// </summary>
    public TimeSpan GetFreeMessagesTimeout { get; set; } = TimeSpan.FromSeconds(20);
}
