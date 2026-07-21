namespace Dex.RfcAbstractions;

/// <summary>
/// Константы формирования RFC 9457 type. Полный type = ProblemTypePrefix + код ошибки.
/// </summary>
public static class RfcTypeConstants
{
    /// <summary>
    /// Префикс RFC 9457 type. Полный type = ProblemTypePrefix + код ошибки (например RfcErrorCodes.NotFound).
    /// </summary>
    public const string ProblemTypePrefix = "/problems/";
}