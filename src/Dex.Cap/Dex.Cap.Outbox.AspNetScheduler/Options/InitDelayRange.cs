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
    /// <summary>Lower bound of the delay range (inclusive).</summary>
    public TimeSpan Min { get; init; }

    /// <summary>Upper bound of the delay range (exclusive when Min &lt; Max).</summary>
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
        var minMs = (int)Min.TotalMilliseconds;
        var maxMs = (int)Max.TotalMilliseconds;

        // Защита от суб-миллисекундного диапазона (Min < Max, но после каста в ms равны): RandomNumberGenerator.GetInt32 требует fromInclusive < toExclusive.
        if (maxMs <= minMs)
        {
            return Min;
        }

        var milliseconds = RandomNumberGenerator.GetInt32(minMs, maxMs);
        return TimeSpan.FromMilliseconds(milliseconds);
    }
}
