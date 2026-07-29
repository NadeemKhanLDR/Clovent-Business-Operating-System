using Clovent.Identity.Branches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Branch"/>. <see cref="Branch.Address"/>
/// maps via <see cref="ValueConverters.AddressConverter"/> (a JSON column) -
/// see that converter's doc comment for why a converter rather than an
/// owned type.
/// </summary>
internal sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches", "Identity");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .HasConversion(ValueConverters.BranchIdConverter)
            .ValueGeneratedNever();

        builder.Property(b => b.CompanyId)
            .HasConversion(ValueConverters.CompanyIdConverter)
            .IsRequired();
        builder.HasIndex(b => b.CompanyId);

        builder.Property(b => b.Name)
            .HasConversion(ValueConverters.BranchNameConverter)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(b => b.Address)
            .HasConversion(ValueConverters.AddressConverter);

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(b => b.CreatedAtUtc).IsRequired();

        builder.Ignore(b => b.DomainEvents);
    }
}
