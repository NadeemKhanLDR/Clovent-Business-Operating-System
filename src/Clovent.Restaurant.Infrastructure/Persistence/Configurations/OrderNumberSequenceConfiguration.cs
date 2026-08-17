using Clovent.Restaurant.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="OrderNumberSequence"/>.</summary>
internal sealed class OrderNumberSequenceConfiguration : IEntityTypeConfiguration<OrderNumberSequence>
{
    public void Configure(EntityTypeBuilder<OrderNumberSequence> builder)
    {
        builder.ToTable("OrderNumberSequences", "Restaurant");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(ValueConverters.OrderNumberSequenceIdConverter)
            .ValueGeneratedNever();

        builder.Property(s => s.Prefix).HasMaxLength(20).IsRequired();
        builder.Property(s => s.NextNumber).IsRequired();

        builder.Ignore(s => s.DomainEvents);
    }
}
