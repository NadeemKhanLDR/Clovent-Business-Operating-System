using Clovent.Catalog.Brands;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Brand"/>.</summary>
internal sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("Brands", "Catalog");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .HasConversion(ValueConverters.BrandIdConverter)
            .ValueGeneratedNever();

        builder.Property(b => b.Name)
            .HasConversion(ValueConverters.BrandNameConverter)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(b => b.CreatedAtUtc).IsRequired();

        builder.Ignore(b => b.DomainEvents);
    }
}
