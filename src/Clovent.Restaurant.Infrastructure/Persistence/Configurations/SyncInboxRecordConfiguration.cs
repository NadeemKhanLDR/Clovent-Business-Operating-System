using Clovent.Platform.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core entity configuration for <see cref="SyncInboxRecord"/>.</summary>
public sealed class SyncInboxRecordConfiguration : IEntityTypeConfiguration<SyncInboxRecord>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<SyncInboxRecord> builder)
    {
        builder.ToTable("SyncInboxRecords", "Restaurant");

        builder.HasKey(r => r.IdempotencyKey);

        builder.Property(r => r.IdempotencyKey)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.PacketId)
            .IsRequired();

        builder.Property(r => r.OrganizationId)
            .IsRequired();

        builder.Property(r => r.BranchId)
            .IsRequired();

        builder.Property(r => r.SourceTerminalId)
            .IsRequired();

        builder.Property(r => r.EntityKind)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.EntityId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.SchemaVersion)
            .IsRequired();

        builder.Property(r => r.PayloadHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(r => r.PayloadJson)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.ReceivedAtUtc)
            .IsRequired();

        builder.Property(r => r.ProcessedAtUtc)
            .IsRequired(false);

        builder.Property(r => r.FailureReason)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(r => r.StagedConflictId)
            .IsRequired(false);

        builder.Property(r => r.ConcurrencyToken)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.HasIndex(r => r.PacketId)
            .HasDatabaseName("IX_SyncInboxRecords_PacketId");

        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_SyncInboxRecords_Status");

        builder.HasIndex(r => new { r.EntityKind, r.EntityId })
            .HasDatabaseName("IX_SyncInboxRecords_EntityKind_EntityId");
    }
}
