using Clovent.MasterData.Departments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.MasterData.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Department"/>.</summary>
internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments", "MasterData");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .HasConversion(ValueConverters.DepartmentIdConverter)
            .ValueGeneratedNever();

        builder.Property(d => d.BranchId)
            .HasConversion(ValueConverters.BranchIdConverter)
            .IsRequired();
        builder.HasIndex(d => d.BranchId);

        builder.Property(d => d.Name)
            .HasConversion(ValueConverters.DepartmentNameConverter)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.CreatedAtUtc).IsRequired();

        builder.Ignore(d => d.DomainEvents);
    }
}
