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
    /// Метод сравнения объектов.
    /// </summary>
    /// <param name="obj">Входной объект.</param>
    /// <returns>true, если объекты равны, иначе false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not Device device)
        {
            return false;
        }

        return Vendor == device.Vendor &&
               Version == device.Version && 
               Product == device.Product && 
               ProcessName == device.ProcessName &&
               EventClassId == device.EventClassId;
    }

    /// <summary>
    /// Метод получения хеш-кода объекта.
    /// </summary>
    /// <returns>Хеш-код на основе свойтв объекта.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Vendor, Version, Product, ProcessName, EventClassId);
    }
}
