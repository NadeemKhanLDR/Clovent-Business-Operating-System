using Clovent.Identity.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Identity.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Organization"/>. <see cref="Organization.CompanyIds"/> maps via a JSON column, mirroring <see cref="UserConfiguration"/>'s <c>RoleIds</c> pattern.</summary>
internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations", "Identity");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .HasConversion(ValueConverters.OrganizationIdConverter)
            .ValueGeneratedNever();

        builder.Property(o => o.Name)
            .HasConversion(ValueConverters.OrganizationNameConverter)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(o => o.TaxId)
            .HasConversion(ValueConverters.TaxIdConverter)
            .HasMaxLength(50);

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.CreatedAtUtc).IsRequired();

        builder.Property(o => o.CompanyIds)
            .HasConversion(ValueConverters.CompanyIdsConverter, ValueConverters.CompanyIdsComparer)
            .IsRequired();

        builder.Ignore(o => o.DomainEvents);
    }
}
