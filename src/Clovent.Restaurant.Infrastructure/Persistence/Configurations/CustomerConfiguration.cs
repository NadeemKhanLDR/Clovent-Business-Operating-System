using Clovent.Restaurant.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Customer"/>.</summary>
internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers", "Restaurant");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(ValueConverters.CustomerIdConverter)
            .ValueGeneratedNever();

        builder.Property(c => c.Code)
            .HasConversion(ValueConverters.EntityCodeConverter)
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(c => c.Code).IsUnique();

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.MobileNumber).HasMaxLength(20).IsRequired();
        builder.Property(c => c.Address).HasMaxLength(500).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(100);
        builder.Property(c => c.OpeningBalance).HasPrecision(18, 2);
        builder.Property(c => c.CreditLimit).HasPrecision(18, 2);
        builder.Property(c => c.OutstandingBalance).HasPrecision(18, 2);
        builder.Property(c => c.IsActive).IsRequired();
        builder.Property(c => c.Notes).HasMaxLength(1000);

        builder.Property(c => c.CreatedAtUtc).IsRequired();
        builder.Property(c => c.UpdatedAtUtc).IsRequired();

        builder.Ignore(c => c.DomainEvents);
    }
}
