using Clovent.Inventory.Adjustments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Inventory.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="StockAdjustment"/>.</summary>
internal sealed class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.ToTable("StockAdjustments", "Inventory");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(ValueConverters.StockAdjustmentIdConverter)
            .ValueGeneratedNever();

        builder.Property(a => a.WarehouseId)
            .HasConversion(ValueConverters.WarehouseIdConverter)
            .IsRequired();
        builder.HasIndex(a => a.WarehouseId);

        builder.Property(a => a.ProductVariantId)
            .HasConversion(ValueConverters.ProductVariantIdConverter)
            .IsRequired();

        builder.Property(a => a.AdjustmentType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.Quantity).HasPrecision(18, 4).IsRequired();
        builder.Property(a => a.Reason).HasMaxLength(500).IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.CreatedAtUtc).IsRequired();
        builder.Property(a => a.AppliedAtUtc);

        builder.Ignore(a => a.DomainEvents);
    }
}
