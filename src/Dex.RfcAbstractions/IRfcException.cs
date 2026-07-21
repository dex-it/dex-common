namespace Dex.RfcAbstractions;

/// <summary>
/// Контракт прикладного исключения для конвертации в RFC 9457 / ProblemDetails.
/// НЕ несёт HTTP-статус и RFC-URI — их резолвит middleware по <see cref="Category"/>.
/// </summary>
public interface IRfcException
{
    /// <summary>Категория проблемы. Единственное обязательное описание природы ошибки.</summary>
    ErrorCategory Category { get; }

    /// <summary>Короткий доменный код без префикса /problems/. null => middleware подставит type по Category.</summary>
    string? ErrorCode => null;

    /// <summary>Стабильный заголовок типа проблемы.</summary>
    string Title { get; }

    /// <summary>Описание конкретного случая. null => detail не попадёт в тело.</summary>
    string? Detail { get; }

    /// <summary>Опциональные машиночитаемые доп-данные для extensions.</summary>
    IReadOnlyDictionary<string, string>? Extensions => null;
}