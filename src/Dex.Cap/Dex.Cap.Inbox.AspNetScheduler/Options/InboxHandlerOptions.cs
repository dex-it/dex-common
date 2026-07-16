using System;

namespace Dex.Cap.Inbox.AspNetScheduler.Options;

/// <summary>
/// Configuration options for the Inbox background handler and cleaner services.
/// </summary>
public sealed class InboxHandlerOptions
{
    /// <summary>
    /// Пауза между циклами обработки. Применяется только когда очередь исчерпана:
    /// пока обработчик забирает полные партии, он продолжает без паузы.
    /// Default 30sec
    /// </summary>
    public TimeSpan Period { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Период запуска чистки обработанных сообщений.
    /// Default 1h
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Ретеншен обработанных сообщений. Одновременно является окном дедупликации:
    /// повторная доставка, пришедшая позже этого срока, будет принята как новое сообщение.
    /// Default 30d
    /// </summary>
    public TimeSpan CleanupOlderThan { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Размер одной пачки удаления при чистке. Default 1000.
    /// </summary>
    public int CleanupBatchSize { get; set; } = 1000;

    /// <summary>
    /// Пауза между пачками удаления. Default 0 (без паузы).
    /// </summary>
    /// <remarks>
    /// Чистка идёт пачками короткими транзакциями, но подряд. Первый проход после накопления окна ретеншена
    /// удаляет разом всё накопившееся, давая всплеск WAL и нагрузку на autovacuum на живом трафике. Ненулевая
    /// пауза размазывает этот первый большой проход во времени. В установившемся режиме удалять нужно примерно
    /// час записей за час, поэтому дефолт нулевой.
    /// </remarks>
    public TimeSpan CleanupBatchDelay { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Initial delay range for the handler (split-brain jitter). Default 5–15s.
    /// </summary>
    public InitDelayRange HandlerInitDelay { get; set; } =
        new() { Min = TimeSpan.FromSeconds(5), Max = TimeSpan.FromSeconds(15) };

    /// <summary>
    /// Initial delay range for the cleaner (split-brain jitter). Default 20–40s.
    /// </summary>
    public InitDelayRange CleanerInitDelay { get; set; } =
        new() { Min = TimeSpan.FromSeconds(20), Max = TimeSpan.FromSeconds(40) };
}