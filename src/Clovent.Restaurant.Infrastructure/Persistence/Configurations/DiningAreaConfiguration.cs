using Clovent.Restaurant.DiningAreas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="DiningArea"/>.</summary>
internal sealed class DiningAreaConfiguration : IEntityTypeConfiguration<DiningArea>
{
    public void Configure(EntityTypeBuilder<DiningArea> builder)
    {
        builder.ToTable("DiningAreas", "Restaurant");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(ValueConverters.DiningAreaIdConverter)
            .ValueGeneratedNever();

        builder.Property(a => a.BranchId)
            .HasConversion(ValueConverters.BranchIdConverter)
            .IsRequired();
        builder.HasIndex(a => a.BranchId);

        builder.Property(a => a.Name)
            .HasConversion(ValueConverters.DiningAreaNameConverter)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.CreatedAtUtc).IsRequired();

        builder.Ignore(a => a.DomainEvents);
    }
}
