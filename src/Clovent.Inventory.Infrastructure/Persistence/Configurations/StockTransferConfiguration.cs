using Clovent.Inventory.Transfers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Inventory.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="StockTransfer"/>.</summary>
internal sealed class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
{
    public void Configure(EntityTypeBuilder<StockTransfer> builder)
    {
        builder.ToTable("StockTransfers", "Inventory");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(ValueConverters.StockTransferIdConverter)
            .ValueGeneratedNever();

        builder.Property(t => t.SourceWarehouseId)
            .HasConversion(ValueConverters.WarehouseIdConverter)
            .IsRequired();

        builder.Property(t => t.DestinationWarehouseId)
            .HasConversion(ValueConverters.WarehouseIdConverter)
            .IsRequired();

        builder.Property(t => t.ProductVariantId)
            .HasConversion(ValueConverters.ProductVariantIdConverter)
            .IsRequired();

        builder.Property(t => t.Quantity).HasPrecision(18, 4).IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.CreatedAtUtc).IsRequired();
        builder.Property(t => t.CompletedAtUtc);

        builder.Ignore(t => t.DomainEvents);
    }
}
