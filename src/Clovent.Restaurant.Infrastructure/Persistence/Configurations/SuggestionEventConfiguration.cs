using Clovent.Restaurant.SmartRecommendations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="SuggestionEvent"/>.</summary>
internal sealed class SuggestionEventConfiguration : IEntityTypeConfiguration<SuggestionEvent>
{
    public void Configure(EntityTypeBuilder<SuggestionEvent> builder)
    {
        builder.ToTable("SuggestionEvents", "Restaurant");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(ValueConverters.SuggestionEventIdConverter)
            .ValueGeneratedNever();

        builder.Property(e => e.OrderId).IsRequired();
        builder.HasIndex(e => e.OrderId);

        builder.Property(e => e.VariantId)
            .HasConversion(ValueConverters.ProductVariantIdConverter)
            .IsRequired();
        builder.HasIndex(e => e.VariantId);

        builder.Property(e => e.TriggerVariantId);
        builder.Property(e => e.Kind).IsRequired();
        builder.HasIndex(e => e.Kind);

        builder.Property(e => e.OrderLineId);
        builder.Property(e => e.AcceptedQuantity).IsRequired();
        builder.Property(e => e.AcceptedUnitAmount).IsRequired();

        builder.Property(e => e.OccurredAtUtc).IsRequired();
        builder.HasIndex(e => e.OccurredAtUtc);
    }
}
