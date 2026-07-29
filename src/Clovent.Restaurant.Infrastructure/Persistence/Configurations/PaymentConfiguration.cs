using Clovent.Restaurant.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Payment"/>.</summary>
internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", "Restaurant");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(ValueConverters.PaymentIdConverter)
            .ValueGeneratedNever();

        builder.Property(p => p.OrderId)
            .HasConversion(ValueConverters.OrderIdConverter)
            .IsRequired();
        builder.HasIndex(p => p.OrderId);

        builder.Property(p => p.PaymentMethodId)
            .HasConversion(ValueConverters.PaymentMethodIdConverter)
            .IsRequired();

        builder.Property(p => p.Amount).HasPrecision(18, 4).IsRequired();
        builder.Property(p => p.IsVoided).IsRequired();

        builder.Property(p => p.CreatedAtUtc).IsRequired();

        builder.Ignore(p => p.DomainEvents);
    }
}
