using System;

namespace Dex.Cap.Outbox.Options;

/// <summary>
/// Настройки постановки и обработки outbox-сообщений.
/// </summary>
/// <remarks>
/// На старте хоста проверяется только <see cref="MaxContentLengthBytes"/>: остальные правила живут в
/// <see cref="OutboxOptionsValidator"/>, который к контейнеру не подключён. Подробности и причина в
/// <c>AddOutbox</c> из пакета <c>Dex.Cap.Outbox.Ef</c>.
/// </remarks>
public class OutboxOptions
{
    /// <summary>
    /// Дефолт для <see cref="MaxContentLengthBytes"/>: 1 МиБ.
    /// </summary>
    public const int DefaultMaxContentLengthBytes = 1024 * 1024;

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
    /// колонка <c>text</c> в PostgreSQL, порядка 1 ГБ. Снять предел целиком можно значением
    /// <see cref="int.MaxValue"/>: <c>Encoding.UTF8.GetByteCount</c> его не превышает. Ноль и отрицательные
    /// значения отвергаются на старте хоста.
    /// <para>
    /// Меряется вывод сериализатора, а <c>DefaultOutboxSerializer</c> оставляет <c>Encoder</c> по умолчанию и
    /// экранирует не-ASCII в <c>\uXXXX</c>. Кириллический символ поэтому расходует 6 байт предела вместо 2 на
    /// проводе, и предел, выставленный по лимиту брокера, окажется примерно втрое строже ожидаемого. Кому
    /// нужно совпадение с размером на проводе, подменяет <c>IOutboxSerializer</c> на построенный поверх
    /// <c>JavaScriptEncoder.UnsafeRelaxedJsonEscaping</c>.
    /// </para>
    /// </remarks>
    public int MaxContentLengthBytes { get; set; } = DefaultMaxContentLengthBytes;
}