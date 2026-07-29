using Clovent.Restaurant.Tables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Table"/>.</summary>
internal sealed class TableConfiguration : IEntityTypeConfiguration<Table>
{
    public void Configure(EntityTypeBuilder<Table> builder)
    {
        builder.ToTable("Tables", "Restaurant");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(ValueConverters.TableIdConverter)
            .ValueGeneratedNever();

        builder.Property(t => t.DiningAreaId)
            .HasConversion(ValueConverters.DiningAreaIdConverter)
            .IsRequired();
        builder.HasIndex(t => t.DiningAreaId);

        builder.Property(t => t.Code)
            .HasConversion(ValueConverters.EntityCodeConverter)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Capacity).IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.OccupancyStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.CreatedAtUtc).IsRequired();

        builder.Ignore(t => t.DomainEvents);
    }
}
