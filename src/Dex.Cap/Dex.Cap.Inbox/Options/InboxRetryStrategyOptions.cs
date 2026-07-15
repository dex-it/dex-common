using System;

namespace Dex.Cap.Inbox.Options;

/// <summary>
/// Вход стратегии повторов.
/// </summary>
/// <param name="StartDate">Точка отсчёта задержки: момент неудачной попытки, а не момент планирования.</param>
/// <param name="CurrentRetry">Номер уже сделанной попытки, начиная с единицы.</param>
public readonly record struct InboxRetryStrategyOptions(DateTime StartDate, int CurrentRetry);
