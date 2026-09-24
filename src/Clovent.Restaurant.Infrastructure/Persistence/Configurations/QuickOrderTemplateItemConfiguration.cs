using Clovent.Restaurant.QuickOrderTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="QuickOrderTemplateItem"/>.</summary>
internal sealed class QuickOrderTemplateItemConfiguration : IEntityTypeConfiguration<QuickOrderTemplateItem>
{
    public void Configure(EntityTypeBuilder<QuickOrderTemplateItem> builder)
    {
        builder.ToTable("QuickOrderTemplateItems", "Restaurant");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id)
            .HasConversion(ValueConverters.QuickOrderTemplateItemIdConverter)
            .ValueGeneratedNever();

        builder.Property(i => i.TemplateId)
            .HasConversion(ValueConverters.QuickOrderTemplateIdConverter)
            .IsRequired();
        builder.HasIndex(i => i.TemplateId);

        builder.Property(i => i.VariantId)
            .HasConversion(ValueConverters.ProductVariantIdConverter)
            .IsRequired();

        builder.Property(i => i.Quantity).HasPrecision(18, 3).IsRequired();
        builder.Property(i => i.TemplateUnitPrice).HasPrecision(18, 4);
    }
}

