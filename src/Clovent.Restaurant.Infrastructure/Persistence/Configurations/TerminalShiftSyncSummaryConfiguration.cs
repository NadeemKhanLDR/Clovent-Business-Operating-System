using Clovent.Restaurant.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core entity configuration for <see cref="TerminalShiftSyncSummary"/>.</summary>
public sealed class TerminalShiftSyncSummaryConfiguration : IEntityTypeConfiguration<TerminalShiftSyncSummary>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<TerminalShiftSyncSummary> builder)
    {
        builder.ToTable("TerminalShiftSyncSummaries", "Restaurant");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.BranchId)
            .IsRequired();

        builder.Property(s => s.TerminalId)
            .IsRequired();

        builder.Property(s => s.ShiftNumber)
            .IsRequired();

        builder.Property(s => s.CashierId)
            .IsRequired();

        builder.Property(s => s.CashierName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.OpenedAtUtc)
            .IsRequired();

        builder.Property(s => s.ClosedAtUtc)
            .IsRequired(false);

        builder.Property(s => s.StartingCash)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(s => s.CountedCash)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(s => s.ExpectedCash)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(s => s.CashVariance)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(s => s.NetSales)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(s => s.TotalOrdersCount)
            .IsRequired();

        builder.Property(s => s.TimestampUtc)
            .IsRequired();

        builder.Property(s => s.ReplicatedAtUtc)
            .IsRequired();

        builder.HasIndex(s => new { s.BranchId, s.TerminalId, s.ShiftNumber })
            .IsUnique()
            .HasDatabaseName("IX_TerminalShiftSyncSummaries_Branch_Terminal_Shift");
    }
}
