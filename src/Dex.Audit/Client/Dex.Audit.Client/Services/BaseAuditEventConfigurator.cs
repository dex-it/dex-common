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
/// Basic Audit Event Configurator.
/// </summary>
/// <param name="auditEventOptions">Audit Event Settings.</param>
public class BaseAuditEventConfigurator(
    IOptions<AuditEventOptions> auditEventOptions)
    : IAuditEventConfigurator
{
    /// <summary>
    /// Configures the audit message to be sent.
    /// </summary>
    /// <param name="auditEventBaseInfo">Basic information about the audit event.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public async Task<AuditEventMessage> ConfigureAuditEventAsync(
        AuditEventBaseInfo auditEventBaseInfo,
        CancellationToken cancellationToken = default)
    {
        var sourceAddress = await GetSourceAddressAsync(cancellationToken)
            .ConfigureAwait(false);
        var deviceInfo = await GetDeviceInfoAsync(cancellationToken)
            .ConfigureAwait(false);
        var userDetails = await GetUserDetailsAsync(cancellationToken)
            .ConfigureAwait(false);

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
    /// Gets information about the address of the event source.
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
    /// Get information about the device of the event source.
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
    /// Get information about the initiator user.
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
