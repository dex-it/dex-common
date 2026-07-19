using System;

namespace Dex.Cap.Outbox.Options;

public class OutboxOptions
{
    /// <summary>
    /// Дефолт для <see cref="MaxContentLength"/>: 1 МиБ.
    /// </summary>
    public const int DefaultMaxContentLength = 1024 * 1024;

    /// <summary>
    /// Количество попыток обработки outbox-сообщений (транзиентные ошибки).
    /// Default: 3
    /// </summary>
    public int Retries { get; set; } = 3;

    /// <summary>
    /// Количество сообщений, захватываемых обработчиком аутбокса из БД за один цикл.
    /// Время обработки ВСЕХ выбранных сообщений отсчитывается с момента их захвата.
    /// Default: 100
    /// </summary>
    public int MessagesToProcess { get; set; } = 100;

    /// <summary>
    /// Степень параллелизма обработки. Рекомендуется, чтобы ConcurrencyLimit не превышал MessagesToProcess.
    /// Default: 1
    /// </summary>
    public int ConcurrencyLimit { get; set; } = 1;

    /// <summary>
    /// Таймаут захвата свободных сообщений из хранилища.
    /// Default: 20sec
    /// </summary>
    public TimeSpan GetFreeMessagesTimeout { get; set; } = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Предельный размер тела сообщения (<see cref="Models.OutboxEnvelope.Content"/>) в байтах (UTF-8),
    /// проверяемый на постановке. Меряется как HTTP-заголовок Content-Length: длина сериализованного тела в
    /// байтах, а не число символов.
    /// Default: 1 МиБ (1048576).
    /// </summary>
    /// <remarks>
    /// Предел задан опцией, а не жёстким <c>HasMaxLength</c> на колонке: так существующим таблицам не нужна
    /// миграция, а потребитель сохраняет контроль. Превышение бросает
    /// <see cref="Exceptions.OutboxContentTooLargeException"/> на постановке, где известен тип сообщения, а не
    /// глубоко в БД. Для потребителей с легитимно большими телами предел поднимают; практический потолок -
    /// колонка <c>text</c> в PostgreSQL, порядка 1 ГБ.
    /// </remarks>
    public int MaxContentLength { get; set; } = DefaultMaxContentLength;
}