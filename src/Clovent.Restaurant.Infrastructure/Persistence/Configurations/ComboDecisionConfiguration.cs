using Clovent.Restaurant.SmartCombos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;
public sealed class ComboDecisionConfiguration : IEntityTypeConfiguration<ComboDecision>
{
    public void Configure(EntityTypeBuilder<ComboDecision> builder)
    {
        builder.ToTable("ComboDecisions", "Restaurant");
        builder.HasKey(x => new { x.WarehouseId, x.Signature });
        builder.Property(x => x.Signature).HasMaxLength(100);
        builder.Property(x => x.Reason).HasMaxLength(250);
        builder.Property(x => x.Version).IsConcurrencyToken();
    }
}
