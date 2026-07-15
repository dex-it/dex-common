using System;

namespace Dex.Cap.Inbox.Interfaces;

/// <summary>
/// Преобразование тела сообщения к строке и обратно.
/// </summary>
/// <remarks>
/// Формат тела это контракт с отправителем и с уже сохранёнными сообщениями: замена сериализатора
/// в работающей системе делает нечитаемыми принятые, но ещё не обработанные записи.
/// </remarks>
public interface IInboxSerializer
{
    /// <summary>
    /// Преобразовать сообщение к строке для хранения.
    /// </summary>
    string Serialize(Type type, object obj);

    /// <summary>
    /// Восстановить сообщение из хранимой строки.
    /// </summary>
    object? Deserialize(Type type, string input);
}
