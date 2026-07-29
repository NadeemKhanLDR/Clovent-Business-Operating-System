using Clovent.Identity.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Identity.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Company"/>. <see cref="Company.BranchIds"/> maps via a JSON column, mirroring <see cref="UserConfiguration"/>'s <c>RoleIds</c> pattern.</summary>
internal sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies", "Identity");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(ValueConverters.CompanyIdConverter)
            .ValueGeneratedNever();

        builder.Property(c => c.OrganizationId)
            .HasConversion(ValueConverters.OrganizationIdConverter)
            .IsRequired();
        builder.HasIndex(c => c.OrganizationId);

        builder.Property(c => c.Name)
            .HasConversion(ValueConverters.CompanyNameConverter)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.TaxId)
            .HasConversion(ValueConverters.TaxIdConverter)
            .HasMaxLength(50);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.CreatedAtUtc).IsRequired();

        builder.Property(c => c.BranchIds)
            .HasConversion(ValueConverters.BranchIdsConverter, ValueConverters.BranchIdsComparer)
            .IsRequired();

        builder.Ignore(c => c.DomainEvents);
    }
}
