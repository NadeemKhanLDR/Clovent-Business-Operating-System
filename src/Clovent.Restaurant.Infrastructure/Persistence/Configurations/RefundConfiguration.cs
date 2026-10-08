using Clovent.Restaurant.Refunds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Refund"/>.</summary>
internal sealed class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("Refunds", "Restaurant");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(ValueConverters.RefundIdConverter)
            .ValueGeneratedNever();

        builder.Property(r => r.RefundNumber)
            .HasConversion(ValueConverters.RefundNumberConverter)
            .HasMaxLength(50)
            .IsRequired();
        builder.HasIndex(r => r.RefundNumber).IsUnique();

        builder.Property(r => r.OrderId)
            .HasConversion(ValueConverters.OrderIdConverter)
            .IsRequired();
        builder.HasIndex(r => r.OrderId);

        builder.Property(r => r.BranchId)
            .HasConversion(ValueConverters.BranchIdConverter)
            .IsRequired();
        builder.HasIndex(r => r.BranchId);

        builder.Property(r => r.WarehouseId)
            .HasConversion(ValueConverters.WarehouseIdConverter)
            .IsRequired();

        builder.Property(r => r.RefundedAtUtc).IsRequired();
        builder.HasIndex(r => r.RefundedAtUtc);

        builder.Property(r => r.CashierId).IsRequired();
        builder.Property(r => r.CashierName).HasMaxLength(200).IsRequired();

        builder.Property(r => r.Reason).HasMaxLength(500).IsRequired();

        builder.Property(r => r.ApprovedByUserId);
        builder.Property(r => r.ApprovedByUserName).HasMaxLength(200);

        builder.Property(r => r.IdempotencyKey).HasMaxLength(100).IsRequired();
        builder.HasIndex(r => r.IdempotencyKey).IsUnique();

        builder.Property(r => r.SubtotalRefunded).HasPrecision(18, 2).IsRequired();
        builder.Property(r => r.DiscountReversedTotal).HasPrecision(18, 2).IsRequired();
        builder.Property(r => r.TaxReversedTotal).HasPrecision(18, 2).IsRequired();
        builder.Property(r => r.GrandTotalRefunded).HasPrecision(18, 2).IsRequired();

        builder.Property(r => r.SettlementMethod)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.SettlementReference).HasMaxLength(100);
        builder.Property(r => r.CustomerId);
        builder.Property(r => r.ReceiptSnapshotJson);
        builder.Property(r => r.CreatedAtUtc).IsRequired();

        builder.HasMany(r => r.Lines)
            .WithOne()
            .HasForeignKey(l => l.RefundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(r => r.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(r => r.DomainEvents);
    }
}
