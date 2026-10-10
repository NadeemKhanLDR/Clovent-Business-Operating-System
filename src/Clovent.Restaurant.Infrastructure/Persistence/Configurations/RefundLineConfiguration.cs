using Clovent.Restaurant.Refunds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="RefundLine"/>.</summary>
internal sealed class RefundLineConfiguration : IEntityTypeConfiguration<RefundLine>
{
    public void Configure(EntityTypeBuilder<RefundLine> builder)
    {
        builder.ToTable("RefundLines", "Restaurant");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasConversion(ValueConverters.RefundLineIdConverter)
            .ValueGeneratedNever();

        builder.Property(l => l.RefundId)
            .HasConversion(ValueConverters.RefundIdConverter)
            .IsRequired();
        builder.HasIndex(l => l.RefundId);

        builder.Property(l => l.OrderLineId)
            .HasConversion(ValueConverters.OrderLineIdConverter)
            .IsRequired();
        builder.HasIndex(l => l.OrderLineId);

        builder.Property(l => l.ProductVariantId)
            .HasConversion(ValueConverters.ProductVariantIdConverter)
            .IsRequired();

        builder.Property(l => l.Sku)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(l => l.Quantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(l => l.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(l => l.DiscountReversed)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(l => l.TaxReversed)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(l => l.LineTotalRefunded)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(l => l.TaxClassification)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(l => l.TaxCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.TaxRatePercentage)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(l => l.TaxIsInclusive)
            .IsRequired();

        builder.Property(l => l.InventoryDisposition)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Ignore(l => l.GrossAmount);
    }
}
