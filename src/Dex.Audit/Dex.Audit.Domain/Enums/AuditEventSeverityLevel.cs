namespace Dex.Audit.Domain.Enums;

/// <summary>
/// Уровень критичности события аудита
/// </summary>
public enum AuditEventSeverityLevel
{
    /// <summary>
    /// Нулевая критичность
    /// </summary>
    Zero = 0,

    /// <summary>
    /// Первый уровень критичности
    /// </summary>
    First = 1,

    /// <summary>
    /// Второй уровень критичности
    /// </summary>
    Second = 2,

    /// <summary>
    /// Третий уровень критичности
    /// </summary>
    Third = 3
}
