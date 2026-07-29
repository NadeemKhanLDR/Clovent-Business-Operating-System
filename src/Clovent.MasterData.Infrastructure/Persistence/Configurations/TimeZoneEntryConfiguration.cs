using Clovent.MasterData.TimeZones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.MasterData.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="TimeZoneEntry"/>.</summary>
internal sealed class TimeZoneEntryConfiguration : IEntityTypeConfiguration<TimeZoneEntry>
{
    public void Configure(EntityTypeBuilder<TimeZoneEntry> builder)
    {
        builder.ToTable("TimeZoneEntries", "MasterData");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(ValueConverters.TimeZoneEntryIdConverter)
            .ValueGeneratedNever();

        builder.Property(t => t.IanaId)
            .HasConversion(ValueConverters.IanaIdConverter)
            .HasMaxLength(100)
            .IsRequired();
        builder.HasIndex(t => t.IanaId).IsUnique();

        builder.Property(t => t.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(t => t.UtcOffsetMinutes).IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.CreatedAtUtc).IsRequired();

        builder.Ignore(t => t.DomainEvents);
    }
}
