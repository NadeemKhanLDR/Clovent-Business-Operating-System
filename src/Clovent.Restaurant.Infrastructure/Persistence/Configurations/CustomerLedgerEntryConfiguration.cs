using Clovent.Restaurant.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="CustomerLedgerEntry"/>.</summary>
internal sealed class CustomerLedgerEntryConfiguration : IEntityTypeConfiguration<CustomerLedgerEntry>
{
    public void Configure(EntityTypeBuilder<CustomerLedgerEntry> builder)
    {
        builder.ToTable("CustomerLedgerEntries", "Restaurant");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(ValueConverters.CustomerLedgerEntryIdConverter)
            .ValueGeneratedNever();

        builder.Property(e => e.CustomerId)
            .HasConversion(ValueConverters.CustomerIdConverter)
            .IsRequired();
        builder.HasIndex(e => e.CustomerId);

        builder.Property(e => e.Date).IsRequired();
        builder.Property(e => e.Reference).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Debit).HasPrecision(18, 2);
        builder.Property(e => e.Credit).HasPrecision(18, 2);
        builder.Property(e => e.RunningBalance).HasPrecision(18, 2);

        builder.Ignore(e => e.DomainEvents);
    }
}
