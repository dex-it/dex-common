using System;

namespace Dex.Cap.Inbox.Exceptions;

/// <summary>
/// Один и тот же тип сообщения присутствует в процессе несколько раз.
/// </summary>
/// <remarks>
/// Сборка типа загружена в несколько контекстов загрузки, поэтому одному дискриминатору отвечают два
/// разных CLR-типа с одинаковой идентичностью. Выбрать между ними нельзя: обработчики в DI
/// зарегистрированы на одну идентичность типа, и выбор второй молча вывел бы эти сообщения из обработки,
/// то есть громкий отказ подменился бы тихим простоем.
/// <para>
/// Тип ошибки отдельный от <see cref="DiscriminatorConflictException"/> намеренно: там ошибка в типах
/// сообщений и чинится их правкой, здесь ошибка в топологии загрузки хоста и чинится тем, что сборку
/// грузят один раз.
/// </para>
/// </remarks>
public class AmbiguousMessageTypeException : InboxException
{
    public AmbiguousMessageTypeException()
    {
    }

    public AmbiguousMessageTypeException(string message) : base(message)
    {
    }

    public AmbiguousMessageTypeException(string message, Exception innerException) : base(message, innerException)
    {
    }
}