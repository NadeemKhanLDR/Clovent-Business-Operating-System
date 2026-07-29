using Clovent.Restaurant.PaymentMethods;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="PaymentMethod"/>.</summary>
internal sealed class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.ToTable("PaymentMethods", "Restaurant");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasConversion(ValueConverters.PaymentMethodIdConverter)
            .ValueGeneratedNever();

        builder.Property(m => m.Name)
            .HasConversion(ValueConverters.PaymentMethodNameConverter)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.CreatedAtUtc).IsRequired();

        builder.Ignore(m => m.DomainEvents);
    }
}
