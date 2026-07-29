using Clovent.Restaurant.KitchenTickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="KitchenTicket"/>.</summary>
internal sealed class KitchenTicketConfiguration : IEntityTypeConfiguration<KitchenTicket>
{
    public void Configure(EntityTypeBuilder<KitchenTicket> builder)
    {
        builder.ToTable("KitchenTickets", "Restaurant");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(ValueConverters.KitchenTicketIdConverter)
            .ValueGeneratedNever();

        builder.Property(t => t.OrderId)
            .HasConversion(ValueConverters.OrderIdConverter)
            .IsRequired();
        builder.HasIndex(t => t.OrderId);

        builder.Property(t => t.OrderLineIds)
            .HasConversion(ValueConverters.KitchenTicketOrderLineIdsConverter, ValueConverters.KitchenTicketOrderLineIdsComparer)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(t => t.Status);

        builder.Property(t => t.CreatedAtUtc).IsRequired();
        builder.Property(t => t.StartedAtUtc);
        builder.Property(t => t.ReadyAtUtc);
        builder.Property(t => t.ServedAtUtc);

        builder.Ignore(t => t.DomainEvents);
    }
}
