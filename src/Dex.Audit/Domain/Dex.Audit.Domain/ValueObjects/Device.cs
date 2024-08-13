namespace Dex.Audit.Domain.ValueObjects;

/// <summary>
/// Информация об устройстве участника системы аудита.
/// </summary>
public class Device
{
    /// <summary>
    /// Название АС.
    /// </summary>
    public string? Vendor { get; init; }

    /// <summary>
    /// Версия АС.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Название журнала аудита АС.
    /// </summary>
    public string? Product { get; init; }
    
    /// <summary>
    /// Полное имя процесса (службы).
    /// </summary>
    public string? ProcessName { get; init; }
    
    /// <summary>
    /// Системный идентификатор сообщения о событии.
    /// </summary>
    public long? EventClassId { get; init; }

    /// <summary>
    /// Получить хэш кода объекта
    /// </summary>
    /// <returns>Хэш сумма</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Vendor, Version, Product, ProcessName, EventClassId);
    }
}
