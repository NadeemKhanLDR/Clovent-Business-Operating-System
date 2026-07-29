using Clovent.Identity.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Role"/>. <see cref="Role.PermissionIds"/>
/// maps the same way <see cref="Users.User.RoleIds"/> does (a JSON column,
/// via the constructor) - see <see cref="ValueConverters.RoleIdsConverter"/>'s
/// doc comment for the shared reasoning; <see cref="ValueConverters.PermissionIdsConverter"/>
/// is its <c>PermissionId</c> counterpart.
/// </summary>
internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "Identity");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(ValueConverters.RoleIdConverter)
            .ValueGeneratedNever();

        builder.Property(r => r.Name)
            .HasConversion(ValueConverters.RoleNameConverter)
            .HasMaxLength(64)
            .IsRequired();
        builder.HasIndex(r => r.Name).IsUnique();

        builder.Property(r => r.CreatedAtUtc).IsRequired();

        builder.Property(r => r.PermissionIds)
            .HasConversion(ValueConverters.PermissionIdsConverter, ValueConverters.PermissionIdsComparer)
            .IsRequired();

        builder.Ignore(r => r.DomainEvents);
    }
}
