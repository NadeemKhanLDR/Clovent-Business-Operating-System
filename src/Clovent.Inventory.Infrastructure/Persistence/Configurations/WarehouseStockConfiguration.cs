using Clovent.Inventory.WarehouseStocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Inventory.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="WarehouseStock"/>.</summary>
internal sealed class WarehouseStockConfiguration : IEntityTypeConfiguration<WarehouseStock>
{
    public void Configure(EntityTypeBuilder<WarehouseStock> builder)
    {
        builder.ToTable("WarehouseStocks", "Inventory");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(ValueConverters.WarehouseStockIdConverter)
            .ValueGeneratedNever();

        builder.Property(s => s.WarehouseId)
            .HasConversion(ValueConverters.WarehouseIdConverter)
            .IsRequired();

        builder.Property(s => s.ProductVariantId)
            .HasConversion(ValueConverters.ProductVariantIdConverter)
            .IsRequired();

        builder.HasIndex(s => new { s.WarehouseId, s.ProductVariantId }).IsUnique();
        builder.HasIndex(s => s.ProductVariantId);

        builder.Property(s => s.QuantityOnHand).HasPrecision(18, 4).IsRequired();
        builder.Property(s => s.QuantityReserved).HasPrecision(18, 4).IsRequired();
        builder.Property(s => s.MinimumStock).HasPrecision(18, 4).IsRequired();
        builder.Property(s => s.MaximumStock).HasPrecision(18, 4).IsRequired();
        builder.Property(s => s.AllowNegativeStock).IsRequired();

        builder.Ignore(s => s.QuantityAvailable);

        builder.Property(s => s.CreatedAtUtc).IsRequired();
        builder.Property(s => s.UpdatedAtUtc).IsRequired();

        builder.Ignore(s => s.DomainEvents);
    }
}
