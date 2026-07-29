using Clovent.Restaurant.OrderLines;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="OrderLine"/>.</summary>
internal sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines", "Restaurant");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasConversion(ValueConverters.OrderLineIdConverter)
            .ValueGeneratedNever();

        builder.Property(l => l.OrderId)
            .HasConversion(ValueConverters.OrderIdConverter)
            .IsRequired();
        builder.HasIndex(l => l.OrderId);

        builder.Property(l => l.ProductVariantId)
            .HasConversion(ValueConverters.ProductVariantIdConverter)
            .IsRequired();

        builder.Property(l => l.Quantity).HasPrecision(18, 4).IsRequired();
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4).IsRequired();
        builder.Property(l => l.TaxRatePercentage).HasPrecision(5, 2).IsRequired();
        builder.Property(l => l.TaxIsInclusive).IsRequired();

        builder.Property(l => l.Notes).HasMaxLength(500);
        builder.Property(l => l.IsVoided).IsRequired();

        builder.Property(l => l.CreatedAtUtc).IsRequired();

        builder.Ignore(l => l.LineTotal);
        builder.Ignore(l => l.DomainEvents);
    }
}
