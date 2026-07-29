using Clovent.Inventory.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Inventory.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="InventoryTransaction"/>.</summary>
internal sealed class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("InventoryTransactions", "Inventory");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(ValueConverters.InventoryTransactionIdConverter)
            .ValueGeneratedNever();

        builder.Property(t => t.WarehouseId)
            .HasConversion(ValueConverters.WarehouseIdConverter)
            .IsRequired();
        builder.HasIndex(t => t.WarehouseId);

        builder.Property(t => t.ProductVariantId)
            .HasConversion(ValueConverters.ProductVariantIdConverter)
            .IsRequired();

        builder.Property(t => t.TransactionType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Quantity).HasPrecision(18, 4).IsRequired();

        builder.Property(t => t.ReferenceType).HasMaxLength(100);
        builder.Property(t => t.ReferenceId);
        builder.Property(t => t.Notes).HasMaxLength(500);

        builder.Property(t => t.OccurredAtUtc)
            .HasConversion(ValueConverters.DateTimeOffsetToUtcTicksConverter)
            .IsRequired();
        builder.HasIndex(t => t.OccurredAtUtc);

        builder.Ignore(t => t.DomainEvents);
    }
}
