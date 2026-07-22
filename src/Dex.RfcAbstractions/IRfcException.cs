namespace Dex.RfcAbstractions;

/// <summary>
/// Контракт прикладного исключения для конвертации в RFC 9457 / ProblemDetails.
/// НЕ несёт HTTP-статус и RFC-URI — их резолвит middleware по <see cref="Category"/>.
/// HTTP-статус выбирается ТОЛЬКО категорией (см. RfcExceptionCategoryMap): произвольные
/// статусы (410, 422, 423, 451 и т.п.) контрактом не выражаются — подберите ближайшую по
/// смыслу <see cref="ErrorCategory"/> (статус возьмётся из неё), при необходимости уточните
/// type через <see cref="ErrorCode"/>.
/// </summary>
public interface IRfcException
{
    /// <summary>
    /// Категория проблемы. Единственное обязательное описание природы ошибки.
    /// Определяет HTTP-статус и (при пустом <see cref="ErrorCode"/>) RFC 9457 type.
    /// </summary>
    ErrorCategory Category { get; }

    /// <summary>
    /// Короткий доменный код без префикса /problems/ (например "card-has-debt").
    /// Ожидаемый формат — lowercase-kebab-сегменты: ^[a-z0-9-]+(/[a-z0-9-]+)*$.
    /// Значение, не подходящее под формат (пробелы, "..", регистр и т.п.), middleware
    /// отбросит и подставит type по <see cref="Category"/>. null/пусто — тоже type по категории.
    /// Не является default-членом намеренно: добавленный только в наследнике DIM-член
    /// не участвует в interface mapping и молча теряется — реализуй явно в каждом типе.
    /// </summary>
    string? ErrorCode { get; }

    /// <summary>
    /// Стабильный заголовок типа проблемы.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Описание конкретного случая. null => detail не попадёт в тело.
    /// ВНИМАНИЕ: значение уходит клиенту во ВСЕХ окружениях, включая Production.
    /// Не помещай сюда stack trace, секреты, строки подключения и внутреннюю диагностику.
    /// </summary>
    string? Detail { get; }

    /// <summary>
    /// Опциональные машиночитаемые доп-данные для extensions. null => доп-данных нет.
    /// Не является default-членом намеренно (симметрично <see cref="ErrorCode"/>):
    /// добавленный только в наследнике DIM-член не участвует в interface mapping и молча
    /// теряется — реализуй явно в каждом типе (можно вернуть null).
    /// ВНИМАНИЕ: значения уходят клиенту во ВСЕХ окружениях, включая Production.
    /// Не помещай сюда секреты и внутреннюю диагностику.
    /// </summary>
    IReadOnlyDictionary<string, string>? Extensions { get; }
}