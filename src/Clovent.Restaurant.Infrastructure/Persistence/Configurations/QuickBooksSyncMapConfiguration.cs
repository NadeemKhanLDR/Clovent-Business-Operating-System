using Clovent.Restaurant.QuickBooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core entity configuration for <see cref="QuickBooksSyncMap"/>.</summary>
public sealed class QuickBooksSyncMapConfiguration : IEntityTypeConfiguration<QuickBooksSyncMap>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<QuickBooksSyncMap> builder)
    {
        builder.ToTable("QuickBooksSyncMaps", "Restaurant");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.LocalEntityId)
            .IsRequired();

        builder.Property(m => m.EntityType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.QuickBooksTxnId)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(m => m.QuickBooksDocNumber)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(m => m.QuickBooksEditSequence)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(m => m.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(m => m.Currency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(m => m.LastError)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(m => m.RetryCount)
            .IsRequired();

        builder.Property(m => m.CreatedAtUtc)
            .IsRequired();

        builder.Property(m => m.SyncedAtUtc)
            .IsRequired(false);

        builder.Property(m => m.RequestPayloadJson)
            .IsRequired(false);

        builder.Property(m => m.ResponsePayloadJson)
            .IsRequired(false);

        builder.Property(m => m.BranchId)
            .IsRequired(false);

        builder.Property(m => m.TerminalId)
            .IsRequired(false);

        builder.Property(m => m.ManagerOverrideNotes)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(m => m.ManagerOverrideUserId)
            .IsRequired(false);

        builder.Property(m => m.ManagerOverrideAtUtc)
            .IsRequired(false);

        // Primary idempotency invariant: Exactly one mapping record per (EntityType, LocalEntityId)
        builder.HasIndex(m => new { m.EntityType, m.LocalEntityId })
            .IsUnique()
            .HasDatabaseName("IX_QuickBooksSyncMaps_EntityType_LocalEntityId");

        builder.HasIndex(m => m.QuickBooksTxnId)
            .HasDatabaseName("IX_QuickBooksSyncMaps_QuickBooksTxnId");

        builder.HasIndex(m => m.Status)
            .HasDatabaseName("IX_QuickBooksSyncMaps_Status");

        builder.HasIndex(m => m.CreatedAtUtc)
            .HasDatabaseName("IX_QuickBooksSyncMaps_CreatedAtUtc");
    }
}
