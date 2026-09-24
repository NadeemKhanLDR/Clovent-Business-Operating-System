using Clovent.Restaurant.SmartRecommendations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="RecommendationRule"/>.</summary>
internal sealed class RecommendationRuleConfiguration : IEntityTypeConfiguration<RecommendationRule>
{
    public void Configure(EntityTypeBuilder<RecommendationRule> builder)
    {
        builder.ToTable("RecommendationRules", "Restaurant");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(ValueConverters.RecommendationRuleIdConverter)
            .ValueGeneratedNever();

        builder.Property(r => r.ProductId);
        builder.HasIndex(r => r.ProductId);

        builder.Property(r => r.RecommendedVariantId)
            .HasConversion(ValueConverters.ProductVariantIdConverter)
            .IsRequired();
        builder.HasIndex(r => r.RecommendedVariantId);

        builder.Property(r => r.Priority).IsRequired();
        builder.HasIndex(r => r.Priority);

        builder.Property(r => r.IsActive).IsRequired();
        builder.HasIndex(r => r.IsActive);

        builder.Property(r => r.StartTime);
        builder.Property(r => r.EndTime);
        builder.Property(r => r.DaysOfWeek);
        builder.Property(r => r.Notes).HasMaxLength(500);

        builder.Property(r => r.CreatedAtUtc).IsRequired();
        builder.Property(r => r.UpdatedAtUtc).IsRequired();

        builder.Ignore(r => r.DomainEvents);
    }
}
