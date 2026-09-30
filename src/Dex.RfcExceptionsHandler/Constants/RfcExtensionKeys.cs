using System.Collections.Frozen;

namespace Dex.RfcExceptionsHandler.Constants;

internal static class RfcExtensionKeys
{
    public const int DefaultExtensionsCapacity = 4;

    // default
    public const string ExceptionType = "exceptionType";
    public const string ExceptionData = "exceptionData";
    public const string StackTrace = "stackTrace";
    public const string TraceId = "traceId";

    /// <summary>
    /// Ключи, которые нельзя перезаписывать кастомными IRfcException.Extensions:
    /// зарезервированные члены RFC 9457 (сериализуются как top-level свойства ProblemDetails,
    /// совпадение даёт дубль-ключи в JSON) и служебные ключи middleware.
    /// Сравнение регистронезависимое: имена RFC 9457 всегда lowercase, но case-insensitive
    /// десериализация потребителя (JsonSerializerDefaults.Web — дефолт ReadFromJsonAsync)
    /// смапит "Type"/"Status" в тот же член с last-wins, обойдя exact-match защиту.
    /// </summary>
    public static readonly FrozenSet<string> ReservedKeys = new[]
    {
        // RFC 9457 top-level члены ProblemDetails
        "type", "title", "status", "detail", "instance",
        // служебные ключи middleware
        ExceptionType, ExceptionData, StackTrace, TraceId
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
}