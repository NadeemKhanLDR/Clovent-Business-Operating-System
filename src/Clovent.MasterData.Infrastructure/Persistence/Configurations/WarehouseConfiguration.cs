using Clovent.MasterData.Warehouses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.MasterData.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Warehouse"/>.</summary>
internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses", "MasterData");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id)
            .HasConversion(ValueConverters.WarehouseIdConverter)
            .ValueGeneratedNever();

        builder.Property(w => w.BranchId)
            .HasConversion(ValueConverters.BranchIdConverter)
            .IsRequired();
        builder.HasIndex(w => w.BranchId);

        builder.Property(w => w.Name)
            .HasConversion(ValueConverters.WarehouseNameConverter)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(w => w.Code)
            .HasConversion(ValueConverters.EntityCodeConverter)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(w => w.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(w => w.CreatedAtUtc).IsRequired();

        builder.Ignore(w => w.DomainEvents);
    }
}
