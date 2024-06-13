namespace Dex.Audit.Domain.ValueObjects;

/// <summary>
/// Информация об устройстве участника системы аудита.
/// </summary>
public class Device
{
    /// <summary>
    /// Название АС.
    /// </summary>
    public string? Vendor { get; set; }

    /// <summary>
    /// Версия АС.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Название журнала аудита АС.
    /// </summary>
    public string? Product { get; set; }
    
    /// <summary>
    /// Полное имя процесса (службы).
    /// </summary>
    public string? ProcessName { get; set; }
    
    /// <summary>
    /// Системный идентификатор сообщения о событии.
    /// </summary>
    public long? EventClassId { get; set; }
}
