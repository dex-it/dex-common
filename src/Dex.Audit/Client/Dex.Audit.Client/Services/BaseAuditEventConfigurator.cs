using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Client.Abstractions.Messages;
using Dex.Audit.Client.Options;
using Dex.Audit.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace Dex.Audit.Client.Services;

/// <summary>
/// Базовый конфигуратор события аудита
/// </summary>
/// <param name="auditEventOptions">Настройки события аудита.</param>
public class BaseAuditEventConfigurator(
    IOptions<AuditEventOptions> auditEventOptions)
    : IAuditEventConfigurator
{
    /// <summary>
    /// Конфигурирует сообщение аудита для отправки
    /// </summary>
    /// <param name="auditEventBaseInfo">Базовая информации о событии аудита</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public async Task<AuditEventMessage> ConfigureAuditEventAsync(
        AuditEventBaseInfo auditEventBaseInfo,
        CancellationToken cancellationToken = default)
    {
        var sourceAddress = await GetSourceAddressAsync(cancellationToken).ConfigureAwait(false);
        var deviceInfo = await GetDeviceInfoAsync(cancellationToken).ConfigureAwait(false);
        var userDetails = await GetUserDetailsAsync(cancellationToken).ConfigureAwait(false);

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
            Start = DateTime.UtcNow,
            SourceGmtDate = DateTime.UtcNow,
            EventType = auditEventBaseInfo.EventType,
            EventObject = auditEventBaseInfo.EventObject,
            Message = auditEventBaseInfo.Message,
            IsSuccess = auditEventBaseInfo.IsSuccess
        };
    }

    /// <summary>
    /// Получает информацию об адресе источника события.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    protected virtual async Task<AddressInfo> GetSourceAddressAsync(
        CancellationToken cancellationToken = default)
    {
        var dnsName = Dns.GetHostName();

        return new AddressInfo
        {
            DnsName = dnsName,
            MacAddress = GetMacAddress(),
            IpAddress = await GetLocalIpAsync(dnsName, cancellationToken)
                .ConfigureAwait(false),
            Host = Environment.MachineName
        };
    }

    /// <summary>
    /// Получает информацию о устройстве источника события
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    protected virtual Task<Device> GetDeviceInfoAsync(CancellationToken cancellationToken = default)
    {
        var assembly = Assembly.GetEntryAssembly();

        return Task.FromResult(new Device
        {
            Vendor = auditEventOptions.Value.SystemName,
            Version = assembly?.GetName().Version?.ToString(),
            ProcessName = assembly?.GetName().Name
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
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
            .Select(nic => nic.GetPhysicalAddress().ToString())
            .FirstOrDefault();
    }

    private static async Task<string?> GetLocalIpAsync(
        string dnsHostName,
        CancellationToken cancellationToken = default)
    {
        var localIPs = await Dns.GetHostAddressesAsync(dnsHostName, cancellationToken)
            .ConfigureAwait(false);
        return localIPs.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString();
    }
}
