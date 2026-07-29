using Clovent.Catalog.Barcodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Barcode"/>.</summary>
internal sealed class BarcodeConfiguration : IEntityTypeConfiguration<Barcode>
{
    public void Configure(EntityTypeBuilder<Barcode> builder)
    {
        builder.ToTable("Barcodes", "Catalog");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .HasConversion(ValueConverters.BarcodeIdConverter)
            .ValueGeneratedNever();

        builder.Property(b => b.ProductVariantId)
            .HasConversion(ValueConverters.ProductVariantIdConverter)
            .IsRequired();
        builder.HasIndex(b => b.ProductVariantId);

        builder.Property(b => b.Value)
            .HasConversion(ValueConverters.BarcodeValueConverter)
            .HasMaxLength(14)
            .IsRequired();
        builder.HasIndex(b => b.Value).IsUnique();

        builder.Property(b => b.IsPrimary).IsRequired();

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(b => b.CreatedAtUtc).IsRequired();

        builder.Ignore(b => b.DomainEvents);
    }
}
