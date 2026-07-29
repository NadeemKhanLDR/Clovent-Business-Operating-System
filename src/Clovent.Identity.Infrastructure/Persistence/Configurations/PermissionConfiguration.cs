using Clovent.Identity.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Identity.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Permission"/> - a flat catalog entry, no collections to reconcile.</summary>
internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions", "Identity");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(ValueConverters.PermissionIdConverter)
            .ValueGeneratedNever();

        builder.Property(p => p.Code)
            .HasConversion(ValueConverters.PermissionCodeConverter)
            .HasMaxLength(260)
            .IsRequired();
        builder.HasIndex(p => p.Code).IsUnique();

        builder.Property(p => p.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(p => p.CreatedAtUtc).IsRequired();

        builder.Ignore(p => p.DomainEvents);
    }
}
