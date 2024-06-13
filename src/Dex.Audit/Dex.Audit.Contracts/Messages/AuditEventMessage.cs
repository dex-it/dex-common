using Dex.Audit.Domain.Enums;
using MassTransit;

namespace Dex.Audit.Contracts.Messages;

/// <summary>
/// Контракт сообщения о событии аудита
/// </summary>
public class AuditEventMessage : IConsumer
{
    /// <summary>
    ///  ID строки о событии ИБ в рамках одного журнала АС
    /// </summary>
    public long ExternalId { get; set; }

    /// <summary>
    /// Название АС-источника информации о событии
    /// </summary>
    public string? DeviceVendor { get; set; }

    /// <summary>
    /// Версия АС-источника информации о событии
    /// </summary>
    public string? DeviceVersion { get; set; }

    /// <summary>
    /// Название журнала аудита АС-источника информации о событии
    /// </summary>
    public string? DeviceProduct { get; set; }

    /// <summary>
    ///  Системное имя (логин) пользователя-инициатора события
    /// </summary>
    public string? SourceUser { get; set; }

    /// <summary>
    /// Домен (или имя рабочей группы) пользователя-инициатора события
    /// </summary>
    public string? SourceUserDomain { get; set; }

    /// <summary>
    /// IP адрес хоста-источника события
    /// </summary>
    public string? SourceIpAddress { get; set; }

    /// <summary>
    /// МАС адрес хоста-источника события
    /// </summary>
    public string? SourceMacAddress { get; set; }

    /// <summary>
    /// DNS-имя источника события
    /// </summary>
    public string? SourceDnsName { get; set; }

    /// <summary>
    /// Netbios имя или hostname хоста-инициатора события
    /// </summary>
    public string? SourceHost { get; set; }

    /// <summary>
    ///  Системное имя (логин) пользователя получателя
    /// </summary>
    public string? DestinationUser { get; set; }

    /// <summary>
    /// Домен (или имя рабочей группы) пользователя получателя
    /// </summary>
    public string? DestinationDomain { get; set; }

    /// <summary>
    ///  IP адрес хоста-получателя
    /// </summary>
    public string? DestinationIpAddress { get; set; }

    /// <summary>
    /// МАС адрес хоста-получателя
    /// </summary>
    public string? DestinationMacAddress { get; set; }

    /// <summary>
    ///  DNS-имя получателя
    /// </summary>
    public string? DestinationDnsName { get; set; }

    /// <summary>
    /// Netbios имя или hostname хоста получателя
    /// </summary>
    public string? DestinationHost { get; set; }

    /// <summary>
    /// Системный идентификатор сообщения о событии
    /// </summary>
    public long? DeviceEventClassId { get; set; }

    /// <summary>
    /// Код события в журнале АС
    /// </summary>
    public string? EventCode { get; set; }

    /// <summary>
    /// Порт на стороне источника
    /// </summary>
    public string? SourcePort { get; set; }

    /// <summary>
    /// Порт на стороне получателя
    /// </summary>
    public string? DestinationPort { get; set; }

    /// <summary>
    /// Протокол на стороне источника
    /// </summary>
    public string? SourceProtocol { get; set; }

    /// <summary>
    ///  Системное время источника события
    /// </summary>
    public DateTime Start { get; set; }

    /// <summary>
    /// Системное время получателя
    /// </summary>
    public DateTime End { get; set; }

    /// <summary>
    /// Время GMT источника события
    /// </summary>
    public DateTime SourceGmtDate { get; set; }

    /// <summary>
    /// Время GMT получателя
    /// </summary>
    public DateTime DestinationGmtDate { get; set; }

    /// <summary>
    /// Объект события 
    /// </summary>
    public string? EventObject { get; set; }

    /// <summary>
    /// Текст сообщения
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Полное имя процесса (службы)
    /// </summary>
    public string? DeviceProcessName { get; set; }

    /// <summary>
    /// Результат (успех/отказ
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Краткое наименование события
    /// </summary>
    public string? EventName { get; set; }

    /// <summary>
    /// Тип события аудита
    /// </summary>
    public string EventType { get; set; }

    /// <summary>
    /// Минимальный уровень важности события для записи, указанный на стороне источника события
    /// </summary>
    public AuditEventSeverityLevel SourceMinSeverityLevel { get; set; }

    /// <summary>
    /// Id настройки аудита
    /// </summary>
    public int? AuditSettingsId { get; set; }
}
