using AutoMapper;
using Dex.Audit.Contracts.Messages;
using Dex.Audit.Domain.Entities;

namespace Dex.Audit.Writer.MapperInfrastructure.Profiles;

/// <summary>
/// Настройки профиля маппинга для моделей, связанных с событиями аудита
/// </summary>
public class AuditEventProfiles : Profile
{
    public AuditEventProfiles()
    {
        CreateMap<AuditEventMessage, AuditEvent>()
            .ForMember(dest => dest.AuditSettings, opt => opt.Ignore())
            .ForPath(dest => dest.Source.GmtDate, opt => opt.MapFrom(src => src.SourceGmtDate))
            .ForPath(dest => dest.Source.Protocol, opt => opt.MapFrom(src => src.SourceProtocol))
            .ForPath(dest => dest.Source.Port, opt => opt.MapFrom(src => src.SourcePort))
            .ForPath(dest => dest.Source.Start, opt => opt.MapFrom(src => src.Start))
            .ForPath(dest => dest.Source.Device.Product, opt => opt.MapFrom(src => src.DeviceProduct))
            .ForPath(dest => dest.Source.Device.Version, opt => opt.MapFrom(src => src.DeviceVersion))
            .ForPath(dest => dest.Source.Device.Vendor, opt => opt.MapFrom(src => src.DeviceVendor))
            .ForPath(dest => dest.Source.Device.ProcessName, opt => opt.MapFrom(src => src.DeviceProcessName))
            .ForPath(dest => dest.Source.Device.EventClassId, opt => opt.MapFrom(src => src.DeviceEventClassId))
            .ForPath(dest => dest.Source.UserDetails.User, opt => opt.MapFrom(src => src.SourceUser))
            .ForPath(dest => dest.Source.UserDetails.UserDomain, opt => opt.MapFrom(src => src.SourceUserDomain))
            .ForPath(dest => dest.Source.AddressInfo.IpAddress, opt => opt.MapFrom(src => src.SourceIpAddress))
            .ForPath(dest => dest.Source.AddressInfo.MacAddress, opt => opt.MapFrom(src => src.SourceMacAddress))
            .ForPath(dest => dest.Source.AddressInfo.DnsName, opt => opt.MapFrom(src => src.SourceDnsName))
            .ForPath(dest => dest.Source.AddressInfo.Host, opt => opt.MapFrom(src => src.SourceHost))
            .ForPath(dest => dest.Destination.GmtDate, opt => opt.MapFrom(src => src.DestinationGmtDate))
            .ForPath(dest => dest.Destination.Port, opt => opt.MapFrom(src => src.DestinationPort))
            .ForPath(dest => dest.Destination.End, opt => opt.MapFrom(src => src.End))
            .ForPath(dest => dest.Destination.UserDetails.User, opt => opt.MapFrom(src => src.DestinationUser))
            .ForPath(dest => dest.Destination.UserDetails.UserDomain, opt => opt.MapFrom(src => src.DestinationDomain))
            .ForPath(dest => dest.Destination.AddressInfo.IpAddress, opt => opt.MapFrom(src => src.DestinationIpAddress))
            .ForPath(dest => dest.Destination.AddressInfo.MacAddress, opt => opt.MapFrom(src => src.DestinationMacAddress))
            .ForPath(dest => dest.Destination.AddressInfo.DnsName, opt => opt.MapFrom(src => src.DestinationDnsName))
            .ForPath(dest => dest.Destination.AddressInfo.Host, opt => opt.MapFrom(src => src.DestinationHost))
            .ForPath(dest => dest.AuditSettingsId, opt => opt.MapFrom(src => src.AuditSettingsId));
    }
}
