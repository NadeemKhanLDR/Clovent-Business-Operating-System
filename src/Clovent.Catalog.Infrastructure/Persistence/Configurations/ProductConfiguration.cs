using Clovent.Catalog.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Product"/>.</summary>
internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", "Catalog");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(ValueConverters.ProductIdConverter)
            .ValueGeneratedNever();

        builder.Property(p => p.Name)
            .HasConversion(ValueConverters.ProductNameConverter)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Sku)
            .HasConversion(ValueConverters.SkuConverter)
            .HasMaxLength(40)
            .IsRequired();
        builder.HasIndex(p => p.Sku).IsUnique();

        builder.Property(p => p.CategoryId)
            .HasConversion(ValueConverters.NullableProductCategoryIdConverter);

        builder.Property(p => p.GroupId)
            .HasConversion(ValueConverters.NullableProductGroupIdConverter);

        builder.Property(p => p.BrandId)
            .HasConversion(ValueConverters.NullableBrandIdConverter);

        builder.Property(p => p.BaseUnitOfMeasureId)
            .HasConversion(ValueConverters.UnitOfMeasureIdConverter)
            .IsRequired();

        builder.Property(p => p.TaxConfiguration)
            .HasConversion(ValueConverters.TaxConfigurationConverter)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.CreatedAtUtc).IsRequired();

        builder.Ignore(p => p.DomainEvents);
    }
}
