using Clovent.Restaurant.Discounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Discount"/>.</summary>
internal sealed class DiscountConfiguration : IEntityTypeConfiguration<Discount>
{
    public void Configure(EntityTypeBuilder<Discount> builder)
    {
        builder.ToTable("Discounts", "Restaurant");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .HasConversion(ValueConverters.DiscountIdConverter)
            .ValueGeneratedNever();

        builder.Property(d => d.OrderId)
            .HasConversion(ValueConverters.OrderIdConverter)
            .IsRequired();
        builder.HasIndex(d => d.OrderId);

        builder.Property(d => d.DiscountType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.Value).HasPrecision(18, 4).IsRequired();
        builder.Property(d => d.Reason).HasMaxLength(500).IsRequired();

        builder.Property(d => d.CreatedAtUtc).IsRequired();

        builder.Ignore(d => d.DomainEvents);
    }
}
