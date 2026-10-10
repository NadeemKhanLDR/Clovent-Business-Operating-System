using Clovent.Platform.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core entity configuration for <see cref="SyncConflictRecord"/>.</summary>
public sealed class SyncConflictRecordConfiguration : IEntityTypeConfiguration<SyncConflictRecord>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<SyncConflictRecord> builder)
    {
        builder.ToTable("SyncConflicts", "Restaurant");

        builder.HasKey(c => c.ConflictId);

        builder.Property(c => c.ConflictId)
            .ValueGeneratedNever();

        builder.Property(c => c.PacketId)
            .IsRequired();

        builder.Ignore(c => c.OriginalPacket);

        builder.Property(c => c.OriginalPacketJson)
            .IsRequired();

        builder.Property(c => c.EntityKind)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.EntityId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.SourceBranchId)
            .IsRequired();

        builder.Property(c => c.SourceTerminalId)
            .IsRequired();

        builder.Property(c => c.IncomingValue)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.CurrentValue)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.IncomingConcurrencyToken)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(c => c.CurrentConcurrencyToken)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(c => c.ConflictType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.ConflictReason)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(c => c.DetectedAtUtc)
            .IsRequired();

        builder.Property(c => c.ReviewedAtUtc)
            .IsRequired(false);

        builder.Property(c => c.ReviewedByUserId)
            .IsRequired(false);

        builder.Property(c => c.ResolutionNotes)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(c => c.ResultingEffect)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.HasIndex(c => c.Status)
            .HasDatabaseName("IX_SyncConflicts_Status");

        builder.HasIndex(c => new { c.EntityKind, c.EntityId })
            .HasDatabaseName("IX_SyncConflicts_EntityKind_EntityId");
    }
}
