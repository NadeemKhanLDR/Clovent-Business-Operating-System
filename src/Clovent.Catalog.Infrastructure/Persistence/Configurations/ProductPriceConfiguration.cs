using Clovent.Catalog.Prices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="ProductPrice"/>.</summary>
internal sealed class ProductPriceConfiguration : IEntityTypeConfiguration<ProductPrice>
{
    public void Configure(EntityTypeBuilder<ProductPrice> builder)
    {
        builder.ToTable("ProductPrices", "Catalog");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(ValueConverters.ProductPriceIdConverter)
            .ValueGeneratedNever();

        builder.Property(p => p.ProductVariantId)
            .HasConversion(ValueConverters.ProductVariantIdConverter)
            .IsRequired();
        builder.HasIndex(p => p.ProductVariantId);

        builder.Property(p => p.PriceType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Amount).HasPrecision(18, 4).IsRequired();

        builder.Property(p => p.CurrencyId)
            .HasConversion(ValueConverters.CurrencyIdConverter)
            .IsRequired();

        builder.Property(p => p.EffectiveFromUtc).IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.CreatedAtUtc).IsRequired();

        builder.Ignore(p => p.DomainEvents);
    }
}
