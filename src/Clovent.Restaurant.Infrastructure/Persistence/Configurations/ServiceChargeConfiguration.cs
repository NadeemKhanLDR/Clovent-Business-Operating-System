using Clovent.Restaurant.ServiceCharges;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="ServiceCharge"/>.</summary>
internal sealed class ServiceChargeConfiguration : IEntityTypeConfiguration<ServiceCharge>
{
    public void Configure(EntityTypeBuilder<ServiceCharge> builder)
    {
        builder.ToTable("ServiceCharges", "Restaurant");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(ValueConverters.ServiceChargeIdConverter)
            .ValueGeneratedNever();

        builder.Property(s => s.OrderId)
            .HasConversion(ValueConverters.OrderIdConverter)
            .IsRequired();
        builder.HasIndex(s => s.OrderId);

        builder.Property(s => s.ServiceChargeType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.Value).HasPrecision(18, 4).IsRequired();
        builder.Property(s => s.Reason).HasMaxLength(500).IsRequired();

        builder.Property(s => s.CreatedAtUtc).IsRequired();

        builder.Ignore(s => s.DomainEvents);
    }
}
