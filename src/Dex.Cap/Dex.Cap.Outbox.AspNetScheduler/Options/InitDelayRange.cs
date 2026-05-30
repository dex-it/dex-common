using System;
using System.Security.Cryptography;

namespace Dex.Cap.Outbox.AspNetScheduler.Options;

/// <summary>
/// Диапазон стартовой задержки (init-delay) фонового сервиса.
/// Min == Max — фиксированная задержка (в т.ч. <see cref="TimeSpan.Zero"/> — без задержки).
/// Инвариант (Min &gt;= 0, Min &lt;= Max) проверяется валидатором опций на старте хоста.
/// </summary>
public sealed class InitDelayRange
{
    public TimeSpan Min { get; init; }
    public TimeSpan Max { get; init; }

    /// <summary>
    /// Возвращает задержку: фиксированную при Min == Max, иначе случайную в [Min, Max).
    /// </summary>
    public TimeSpan GetDelay()
    {
        if (Min == Max)
        {
            return Min;
        }

        // int-каст безопасен: значения init-delay задаются в секундах, переполнение int возникло бы лишь при Max > ~24.8 дней, что отклоняется валидатором опций.
        var milliseconds = RandomNumberGenerator.GetInt32((int)Min.TotalMilliseconds, (int)Max.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(milliseconds);
    }
}
