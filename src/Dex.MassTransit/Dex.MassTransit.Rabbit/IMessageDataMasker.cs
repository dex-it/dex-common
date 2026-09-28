using System;

namespace Dex.MassTransit.Rabbit;

/// <summary>
/// Маскирует тело сообщения перед записью в лог об упавшей обработке.
/// </summary>
/// <remarks>
/// Берётся из контейнера консьюмера: достаточно зарегистрировать реализацию, консьюмеры не меняются.
/// Предел размера держит пакет — сюда приходит уже усечённое начало тела, и метку усечения ставит
/// тоже пакет. Сбой маскировщика тело не открывает: вместо него в запись уходит маркер.
/// </remarks>
public interface IMessageDataMasker
{
    /// <summary>
    /// Отдаёт тело для лога с замаскированными значениями.
    /// </summary>
    /// <param name="json">Тело в JSON, UTF-8, не длиннее <paramref name="limit"/>; обрез идёт по границе символа.</param>
    /// <param name="isComplete">Ложь — передано только начало тела, последний токен может быть оборван.</param>
    /// <param name="limit">Предельный размер результата в байтах UTF-8.</param>
    string Mask(ReadOnlySpan<byte> json, bool isComplete, int limit);
}
