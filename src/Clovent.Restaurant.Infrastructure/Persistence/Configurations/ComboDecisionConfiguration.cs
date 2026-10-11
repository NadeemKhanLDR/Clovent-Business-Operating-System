using Clovent.Restaurant.SmartCombos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;
/// <summary>EF Core entity configuration for <see cref="ComboDecision"/>.</summary>
public sealed class ComboDecisionConfiguration : IEntityTypeConfiguration<ComboDecision>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ComboDecision> builder)
    {
        builder.ToTable("ComboDecisions", "Restaurant");
        builder.HasKey(x => new { x.WarehouseId, x.Signature });
        builder.Property(x => x.Signature).HasMaxLength(100);
        builder.Property(x => x.Reason).HasMaxLength(250);
        builder.Property(x => x.Version).IsConcurrencyToken();
    }
}
