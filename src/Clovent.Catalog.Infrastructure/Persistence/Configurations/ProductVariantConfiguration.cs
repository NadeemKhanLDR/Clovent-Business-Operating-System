using Clovent.Catalog.Variants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="ProductVariant"/>.</summary>
internal sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants", "Catalog");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id)
            .HasConversion(ValueConverters.ProductVariantIdConverter)
            .ValueGeneratedNever();

        builder.Property(v => v.ProductId)
            .HasConversion(ValueConverters.ProductIdConverter)
            .IsRequired();
        builder.HasIndex(v => v.ProductId);

        builder.Property(v => v.Name)
            .HasConversion(ValueConverters.VariantNameConverter)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(v => v.Sku)
            .HasConversion(ValueConverters.SkuConverter)
            .HasMaxLength(40)
            .IsRequired();
        builder.HasIndex(v => v.Sku).IsUnique();

        builder.Property(v => v.UnitOfMeasureId)
            .HasConversion(ValueConverters.UnitOfMeasureIdConverter)
            .IsRequired();

        builder.Property(v => v.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(v => v.CreatedAtUtc).IsRequired();

        builder.Ignore(v => v.DomainEvents);
    }
}
