using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using Dex.Audit.Client.Interfaces;
using Dex.Audit.Client.Messages;
using Dex.Audit.Client.Options;
using Dex.Audit.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace Dex.Audit.Client.Services;

/// <summary>
/// Базовый конфигуратор события аудита
/// </summary>
public class BaseAuditEventConfigurator : IAuditEventConfigurator
{
    private readonly AuditEventOptions _auditEventOptions;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BaseAuditEventConfigurator"/>.
    /// </summary>
    /// <param name="auditEventOptions">Настройки события аудита.</param>
    public BaseAuditEventConfigurator(IOptions<AuditEventOptions> auditEventOptions)
    {
        _auditEventOptions = auditEventOptions.Value;
    }

    /// <summary>
    /// Конфигурирует сообщение аудита для отправки
    /// </summary>
    /// <param name="auditEventBaseInfo">Базовая информации о событии аудита</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public async Task<AuditEventMessage> ConfigureAuditEventAsync(AuditEventBaseInfo auditEventBaseInfo, CancellationToken cancellationToken = default)
    {
        AddressInfo sourceAddress = await GetSourceAddressAsync(cancellationToken);
        Device deviceInfo = await GetDeviceInfoAsync(cancellationToken);
        UserDetails userDetails = await GetUserDetailsAsync(cancellationToken);

        return new AuditEventMessage
        {
            DeviceVendor = deviceInfo.Vendor,
            DeviceVersion = deviceInfo.Version,
            DeviceProcessName = deviceInfo.ProcessName,
            SourceUser = userDetails.User,
            SourceUserDomain = userDetails.UserDomain,
            SourceIpAddress = sourceAddress.IpAddress,
            SourceMacAddress = sourceAddress.MacAddress,
            SourceDnsName = sourceAddress.DnsName,
            SourceHost = sourceAddress.Host,
            Start = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc),
            SourceGmtDate = DateTime.UtcNow,
            EventType = auditEventBaseInfo.EventType,
            EventName = auditEventBaseInfo.EventType,
            EventObject = auditEventBaseInfo.EventObject,
            Message = auditEventBaseInfo.Message,
            IsSuccess = auditEventBaseInfo.Success
        };
    }

    /// <summary>
    /// Получает информацию об адресе источника события 
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    protected virtual async Task<AddressInfo> GetSourceAddressAsync(CancellationToken cancellationToken = default)
    {
        string dnsName = Dns.GetHostName();

        return new AddressInfo
        {
            DnsName = dnsName,
            MacAddress = GetMacAddress(),
            IpAddress = await GetLocalIpAsync(dnsName, cancellationToken),
            Host = Environment.MachineName
        };
    }

    /// <summary>
    /// Получает информацию о устройстве источника события
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    protected virtual Task<Device> GetDeviceInfoAsync(CancellationToken cancellationToken = default)
    {
        Assembly? assembly = Assembly.GetEntryAssembly();

        return Task.FromResult(new Device
        {
            Vendor = _auditEventOptions.SystemName, Version = assembly?.GetName().Version?.ToString(), ProcessName = assembly?.GetName().Name
        });
    }

    /// <summary>
    /// Получает информацию о пользователе-инициаторе
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    protected virtual Task<UserDetails> GetUserDetailsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new UserDetails());
    }

    private static string? GetMacAddress()
    {
        return
        (
            from nic in NetworkInterface.GetAllNetworkInterfaces()
            where nic.OperationalStatus == OperationalStatus.Up
            select nic.GetPhysicalAddress().ToString()
        ).FirstOrDefault();
    }

    private static async Task<string?> GetLocalIpAsync(string dnsHostName, CancellationToken cancellationToken = default)
    {
        IPAddress[] localIPs = await Dns.GetHostAddressesAsync(dnsHostName, cancellationToken);
        return localIPs.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString();
    }
}
