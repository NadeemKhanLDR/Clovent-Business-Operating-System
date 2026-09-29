using Clovent.Restaurant.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="CustomerPaymentAllocation"/>.</summary>
internal sealed class CustomerPaymentAllocationConfiguration : IEntityTypeConfiguration<CustomerPaymentAllocation>
{
    public void Configure(EntityTypeBuilder<CustomerPaymentAllocation> builder)
    {
        builder.ToTable("CustomerPaymentAllocations", "Restaurant");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(ValueConverters.CustomerPaymentAllocationIdConverter)
            .ValueGeneratedNever();

        builder.Property(a => a.CustomerId)
            .HasConversion(ValueConverters.CustomerIdConverter)
            .IsRequired();
        builder.HasIndex(a => a.CustomerId);

        builder.Property(a => a.OrderId)
            .HasConversion(ValueConverters.OrderIdConverter)
            .IsRequired();
        builder.HasIndex(a => a.OrderId);

        builder.Property(a => a.CustomerLedgerEntryId)
            .HasConversion(ValueConverters.NullableCustomerLedgerEntryIdConverter)
            .IsRequired(false);
        builder.HasIndex(a => a.CustomerLedgerEntryId);

        builder.Property(a => a.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(a => a.AllocatedAtUtc).IsRequired();
        builder.Property(a => a.Notes).HasMaxLength(500);

        builder.Ignore(a => a.DomainEvents);
    }
}
