using Clovent.Restaurant.QuickOrderTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="QuickOrderTemplate"/>.</summary>
internal sealed class QuickOrderTemplateConfiguration : IEntityTypeConfiguration<QuickOrderTemplate>
{
    public void Configure(EntityTypeBuilder<QuickOrderTemplate> builder)
    {
        builder.ToTable("QuickOrderTemplates", "Restaurant");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(ValueConverters.QuickOrderTemplateIdConverter)
            .ValueGeneratedNever();

        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.IsActive).IsRequired();
        builder.HasIndex(t => t.IsActive);
        builder.Property(t => t.DisplayOrder).IsRequired();

        builder.Property(t => t.CreatedAtUtc).IsRequired();
        builder.Property(t => t.UpdatedAtUtc).IsRequired();

        builder.HasMany(t => t.Items)
            .WithOne()
            .HasForeignKey(i => i.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(t => t.DomainEvents);
    }
}
