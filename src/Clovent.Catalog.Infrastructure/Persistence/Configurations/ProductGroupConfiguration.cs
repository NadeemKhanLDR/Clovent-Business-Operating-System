using Clovent.Catalog.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="ProductGroup"/>.</summary>
internal sealed class ProductGroupConfiguration : IEntityTypeConfiguration<ProductGroup>
{
    public void Configure(EntityTypeBuilder<ProductGroup> builder)
    {
        builder.ToTable("ProductGroups", "Catalog");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id)
            .HasConversion(ValueConverters.ProductGroupIdConverter)
            .ValueGeneratedNever();

        builder.Property(g => g.Name)
            .HasConversion(ValueConverters.ProductGroupNameConverter)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(g => g.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(g => g.CreatedAtUtc).IsRequired();

        builder.Ignore(g => g.DomainEvents);
    }
}
