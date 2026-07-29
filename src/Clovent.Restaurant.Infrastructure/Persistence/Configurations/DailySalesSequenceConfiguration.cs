using Clovent.Restaurant.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="DailySalesSequence"/>.</summary>
internal sealed class DailySalesSequenceConfiguration : IEntityTypeConfiguration<DailySalesSequence>
{
    public void Configure(EntityTypeBuilder<DailySalesSequence> builder)
    {
        builder.ToTable("DailySalesSequences", "Restaurant");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(ValueConverters.DailySalesSequenceIdConverter)
            .ValueGeneratedNever();

        builder.Property(s => s.WarehouseId)
            .HasConversion(ValueConverters.WarehouseIdConverter)
            .IsRequired();

        builder.Property(s => s.Date).IsRequired();
        builder.HasIndex(s => new { s.WarehouseId, s.Date }).IsUnique();

        builder.Property(s => s.LastNumber).IsRequired();

        builder.Ignore(s => s.DomainEvents);
    }
}
