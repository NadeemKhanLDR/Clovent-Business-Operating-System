using Clovent.Catalog.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="ProductCategory"/>.</summary>
internal sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("ProductCategories", "Catalog");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(ValueConverters.ProductCategoryIdConverter)
            .ValueGeneratedNever();

        builder.Property(c => c.Name)
            .HasConversion(ValueConverters.ProductCategoryNameConverter)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.ParentCategoryId)
            .HasConversion(ValueConverters.NullableProductCategoryIdConverter);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.ColorHex).HasMaxLength(7);
        builder.Property(c => c.SortOrder).IsRequired();

        builder.Property(c => c.CreatedAtUtc).IsRequired();

        builder.Ignore(c => c.DomainEvents);
    }
}
