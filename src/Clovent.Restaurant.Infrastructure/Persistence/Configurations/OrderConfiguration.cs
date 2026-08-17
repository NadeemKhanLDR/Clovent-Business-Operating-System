using Clovent.Restaurant.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Order"/>.</summary>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", "Restaurant");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .HasConversion(ValueConverters.OrderIdConverter)
            .ValueGeneratedNever();

        builder.Property(o => o.OrderNumber)
            .HasConversion(ValueConverters.OrderNumberConverter)
            .HasMaxLength(40)
            .IsRequired();
        builder.HasIndex(o => o.OrderNumber).IsUnique();

        builder.Property(o => o.DailySalesNumber);

        builder.Property(o => o.OrderType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(o => o.Status);

        builder.Property(o => o.TableId)
            .HasConversion(ValueConverters.NullableTableIdConverter);
        builder.HasIndex(o => o.TableId);

        builder.Property(o => o.CustomerId)
            .HasConversion(ValueConverters.NullableCustomerIdConverter);
        builder.HasIndex(o => o.CustomerId);

        builder.Property(o => o.WarehouseId)
            .HasConversion(ValueConverters.WarehouseIdConverter)
            .IsRequired();

        builder.Property(o => o.Notes).HasMaxLength(1000);
        builder.Property(o => o.CustomerNotes).HasMaxLength(1000);

        builder.Property(o => o.OrderLineIds)
            .HasConversion(ValueConverters.OrderLineIdsConverter, ValueConverters.OrderLineIdsComparer)
            .IsRequired();

        builder.Property(o => o.DiscountIds)
            .HasConversion(ValueConverters.DiscountIdsConverter, ValueConverters.DiscountIdsComparer)
            .IsRequired();

        builder.Property(o => o.ServiceChargeIds)
            .HasConversion(ValueConverters.ServiceChargeIdsConverter, ValueConverters.ServiceChargeIdsComparer)
            .IsRequired();

        builder.Property(o => o.PaymentIds)
            .HasConversion(ValueConverters.PaymentIdsConverter, ValueConverters.PaymentIdsComparer)
            .IsRequired();

        builder.Property(o => o.CreatedAtUtc).IsRequired();
        builder.Property(o => o.UpdatedAtUtc).IsRequired();

        builder.Ignore(o => o.DomainEvents);
    }
}
