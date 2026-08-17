using Clovent.Restaurant.ActivityLogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="ActivityLogEntry"/>.</summary>
internal sealed class ActivityLogEntryConfiguration : IEntityTypeConfiguration<ActivityLogEntry>
{
    public void Configure(EntityTypeBuilder<ActivityLogEntry> builder)
    {
        builder.ToTable("ActivityLogEntries", "Restaurant");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(ValueConverters.ActivityLogEntryIdConverter)
            .ValueGeneratedNever();

        builder.Property(e => e.Action).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Details).HasMaxLength(1000);
        builder.Property(e => e.PerformedBy).HasMaxLength(200).IsRequired();
        builder.Property(e => e.MachineName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.OccurredAtUtc).IsRequired();
        builder.HasIndex(e => e.OccurredAtUtc);

        builder.Ignore(e => e.DomainEvents);
    }
}
