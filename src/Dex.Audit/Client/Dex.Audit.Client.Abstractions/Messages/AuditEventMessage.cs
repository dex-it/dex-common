using Dex.Audit.Domain.Enums;

namespace Dex.Audit.Client.Abstractions.Messages;

/// <summary>
/// Контракт сообщения о событии аудита
/// </summary>
public class AuditEventMessage
{
    /// <summary>
    /// Название АС-источника информации о событии.
    /// </summary>
    public string? DeviceVendor { get; init; }

    /// <summary>
    /// Версия АС-источника информации о событии.
    /// </summary>
    public string? DeviceVersion { get; init; }

    /// <summary>
    /// Название журнала аудита АС-источника информации о событии.
    /// </summary>
    public string? DeviceProduct { get; set; }

    /// <summary>
    ///  Системное имя (логин) пользователя-инициатора события.
    /// </summary>
    public string? SourceUser { get; init; }

    /// <summary>
    /// Домен (или имя рабочей группы) пользователя-инициатора события.
    /// </summary>
    public string? SourceUserDomain { get; init; }

    /// <summary>
    /// IP адрес хоста-источника события.
    /// </summary>
    public string? SourceIpAddress { get; init; }

    /// <summary>
    /// МАС адрес хоста-источника события.
    /// </summary>
    public string? SourceMacAddress { get; init; }

    /// <summary>
    /// DNS-имя источника события.
    /// </summary>
    public string? SourceDnsName { get; init; }

    /// <summary>
    /// Netbios имя или hostname хоста-инициатора события.
    /// </summary>
    public string? SourceHost { get; init; }

    /// <summary>
    ///  Системное имя (логин) пользователя получателя.
    /// </summary>
    public string? DestinationUser { get; set; }

    /// <summary>
    /// Домен (или имя рабочей группы) пользователя получателя.
    /// </summary>
    public string? DestinationDomain { get; set; }

    /// <summary>
    ///  IP адрес хоста-получателя.
    /// </summary>
    public string? DestinationIpAddress { get; set; }

    /// <summary>
    /// МАС адрес хоста-получателя.
    /// </summary>
    public string? DestinationMacAddress { get; set; }

    /// <summary>
    ///  DNS-имя получателя.
    /// </summary>
    public string? DestinationDnsName { get; set; }

    /// <summary>
    /// Netbios имя или hostname хоста получателя.
    /// </summary>
    public string? DestinationHost { get; set; }

    /// <summary>
    /// Системный идентификатор сообщения о событии.
    /// </summary>
    public long? DeviceEventClassId { get; set; }

    /// <summary>
    /// Порт на стороне источника.
    /// </summary>
    public string? SourcePort { get; set; }

    /// <summary>
    /// Порт на стороне получателя.
    /// </summary>
    public string? DestinationPort { get; set; }

    /// <summary>
    /// Протокол на стороне источника.
    /// </summary>
    public string? SourceProtocol { get; set; }

    /// <summary>
    ///  Системное время источника события.
    /// </summary>
    public DateTime Start { get; init; }

    /// <summary>
    /// Системное время получателя.
    /// </summary>
    public DateTime End { get; set; }

    /// <summary>
    /// Время GMT источника события.
    /// </summary>
    public DateTime SourceGmtDate { get; init; }

    /// <summary>
    /// Время GMT получателя.
    /// </summary>
    public DateTime DestinationGmtDate { get; set; }

    /// <summary>
    /// Объект события.
    /// </summary>
    public string? EventObject { get; init; }

    /// <summary>
    /// Текст сообщения.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Полное имя процесса (службы).
    /// </summary>
    public string? DeviceProcessName { get; init; }

    /// <summary>
    /// Результат (успех/отказ).
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Тип события аудита.
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// Минимальный уровень важности события для записи, указанный на стороне источника события.
    /// </summary>
    public AuditEventSeverityLevel SourceMinSeverityLevel { get; set; }

    /// <summary>
    /// Id настройки аудита.
    /// </summary>
    public Guid? AuditSettingsId { get; set; }
}
