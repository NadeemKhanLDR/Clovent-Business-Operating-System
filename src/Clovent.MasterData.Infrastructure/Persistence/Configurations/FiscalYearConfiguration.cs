using Clovent.MasterData.FiscalYears;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.MasterData.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="FiscalYear"/>.</summary>
internal sealed class FiscalYearConfiguration : IEntityTypeConfiguration<FiscalYear>
{
    public void Configure(EntityTypeBuilder<FiscalYear> builder)
    {
        builder.ToTable("FiscalYears", "MasterData");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id)
            .HasConversion(ValueConverters.FiscalYearIdConverter)
            .ValueGeneratedNever();

        builder.Property(f => f.OrganizationId)
            .HasConversion(ValueConverters.OrganizationIdConverter)
            .IsRequired();
        builder.HasIndex(f => f.OrganizationId);

        builder.Property(f => f.Name)
            .HasConversion(ValueConverters.FiscalYearNameConverter)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(f => f.StartDate).IsRequired();
        builder.Property(f => f.EndDate).IsRequired();

        builder.Property(f => f.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(f => f.CreatedAtUtc).IsRequired();

        builder.Ignore(f => f.DomainEvents);
    }
}
