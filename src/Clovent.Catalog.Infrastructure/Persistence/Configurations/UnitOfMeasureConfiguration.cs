using Clovent.Catalog.UnitsOfMeasure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="UnitOfMeasure"/>.</summary>
internal sealed class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.ToTable("UnitsOfMeasure", "Catalog");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasConversion(ValueConverters.UnitOfMeasureIdConverter)
            .ValueGeneratedNever();

        builder.Property(u => u.Code)
            .HasConversion(ValueConverters.UnitOfMeasureCodeConverter)
            .HasMaxLength(10)
            .IsRequired();
        builder.HasIndex(u => u.Code).IsUnique();

        builder.Property(u => u.Name).HasMaxLength(100).IsRequired();

        builder.Property(u => u.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.CreatedAtUtc).IsRequired();

        builder.Ignore(u => u.DomainEvents);
    }
}
