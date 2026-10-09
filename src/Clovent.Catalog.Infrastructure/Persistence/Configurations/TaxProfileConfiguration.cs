using Clovent.Catalog.TaxProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="TaxProfile"/>.</summary>
internal sealed class TaxProfileConfiguration : IEntityTypeConfiguration<TaxProfile>
{
    public void Configure(EntityTypeBuilder<TaxProfile> builder)
    {
        builder.ToTable("TaxProfiles", "Catalog");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(ValueConverters.TaxProfileIdConverter)
            .ValueGeneratedNever();

        builder.Property(p => p.Code)
            .HasMaxLength(50)
            .IsRequired();
        builder.HasIndex(p => p.Code).IsUnique();

        builder.Property(p => p.InvoiceDisplayName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(p => p.Authority)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Jurisdiction)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.TaxClassification)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.ItemClassification)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.RatePercentage)
            .HasColumnType("decimal(18, 4)")
            .IsRequired();

        builder.Property(p => p.PricingMode)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.EffectiveFromUtc)
            .IsRequired();

        builder.Property(p => p.EffectiveToUtc);

        builder.Property(p => p.IsActive)
            .IsRequired();

        builder.Property(p => p.ReferenceDocument)
            .HasMaxLength(255);

        builder.Property(p => p.PaymentMethodRestriction)
            .HasMaxLength(100);

        builder.Property(p => p.Notes)
            .HasMaxLength(500);

        builder.Property(p => p.CreatedAtUtc)
            .IsRequired();

        builder.Property(p => p.UpdatedAtUtc)
            .IsRequired();

        builder.Ignore(p => p.DomainEvents);
        builder.Ignore(p => p.IsInclusive);
    }
}
