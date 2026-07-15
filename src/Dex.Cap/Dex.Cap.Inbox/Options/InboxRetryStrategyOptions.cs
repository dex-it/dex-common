using System;

namespace Dex.Cap.Inbox.Options;

/// <summary>
/// Вход стратегии повторов.
/// </summary>
/// <param name="StartDate">Точка отсчёта задержки: момент неудачной попытки, а не момент планирования.</param>
/// <param name="CurrentRetry">Номер уже сделанной попытки.</param>
public readonly record struct InboxRetryStrategyOptions(DateTime? StartDate, int? CurrentRetry = null)
{
    /// <summary>
    /// Точка отсчёта задержки: момент неудачной попытки, а не момент планирования.
    /// </summary>
    public DateTime? StartDate { get; } = StartDate;

    /// <summary>
    /// Номер уже сделанной попытки.
    /// </summary>
    public int? CurrentRetry { get; } = CurrentRetry;
}
