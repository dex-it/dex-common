using Dex.Audit.Domain.Entities;
using Dex.Audit.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dex.Audit.EF.EntityConfiguration;

public class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    private const string SourcePropertyPrefix = "Source";
    private const string DestinationPropertyPrefix = "Destination";
    private const string DevicePrefix = "Device";

    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("AuditEvents").HasKey(e => e.ExternalId);

        builder.OwnsOne(root => root.Source, source =>
        {
            source.OwnsOne(a => a.Device, device =>
            {
                device.Property(x => x.Vendor).HasColumnName(DevicePrefix + nameof(Device.Vendor));
                device.Property(x => x.Version).HasColumnName(DevicePrefix + nameof(Device.Version));
                device.Property(x => x.Product).HasColumnName(DevicePrefix + nameof(Device.Product));
                device.Property(x => x.EventClassId).HasColumnName(DevicePrefix + nameof(Device.EventClassId));
                device.Property(x => x.ProcessName).HasColumnName(DevicePrefix + nameof(Device.ProcessName));
            });

            source.OwnsOne(a => a.UserDetails, userDetails =>
            {
                userDetails.Property(x => x.User).HasColumnName(SourcePropertyPrefix + nameof(Source.UserDetails.User));
                userDetails.Property(x => x.UserDomain).HasColumnName(SourcePropertyPrefix + nameof(Source.UserDetails.UserDomain));
            });

            source.OwnsOne(a => a.AddressInfo, addressInfo =>
            {
                addressInfo.Property(x => x.IpAddress).HasColumnName(SourcePropertyPrefix + nameof(Source.AddressInfo.IpAddress));
                addressInfo.Property(x => x.MacAddress).HasColumnName(SourcePropertyPrefix + nameof(Source.AddressInfo.MacAddress));
                addressInfo.Property(x => x.DnsName).HasColumnName(SourcePropertyPrefix + nameof(Source.AddressInfo.DnsName));
                addressInfo.Property(x => x.Host).HasColumnName(SourcePropertyPrefix + nameof(Source.AddressInfo.Host));
            });

            source.Property(e => e.Port).HasColumnName(SourcePropertyPrefix + nameof(Source.Port));
            source.Property(e => e.GmtDate).HasColumnName(SourcePropertyPrefix + nameof(Source.GmtDate));
            source.Property(e => e.Start).HasColumnName(SourcePropertyPrefix + nameof(AuditEvent.Source.Start));
            source.Property(e => e.Protocol).HasColumnName(SourcePropertyPrefix + nameof(Source.Protocol));
        });

        builder.OwnsOne(root => root.Destination, destination =>
        {
            destination.OwnsOne(a => a.UserDetails, userDetails =>
            {
                userDetails.Property(x => x.User).HasColumnName(DestinationPropertyPrefix + nameof(Destination.UserDetails.User));
                userDetails.Property(x => x.UserDomain).HasColumnName(DestinationPropertyPrefix + nameof(Destination.UserDetails.UserDomain));
            });

            destination.OwnsOne(a => a.AddressInfo, addressInfo =>
            {
                addressInfo.Property(x => x.IpAddress).HasColumnName(DestinationPropertyPrefix + nameof(Destination.AddressInfo.IpAddress));
                addressInfo.Property(x => x.MacAddress).HasColumnName(DestinationPropertyPrefix + nameof(Destination.AddressInfo.MacAddress));
                addressInfo.Property(x => x.DnsName).HasColumnName(DestinationPropertyPrefix + nameof(Destination.AddressInfo.DnsName));
                addressInfo.Property(x => x.Host).HasColumnName(DestinationPropertyPrefix + nameof(Destination.AddressInfo.Host));
            });

            destination.Property(e => e.Port).HasColumnName(DestinationPropertyPrefix + nameof(Destination.Port));
            destination.Property(e => e.GmtDate).HasColumnName(DestinationPropertyPrefix + nameof(Destination.GmtDate));
            destination.Property(e => e.End).HasColumnName(DestinationPropertyPrefix + nameof(AuditEvent.Destination.End));
        });
    }
}
