using Clovent.Identity.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="User"/>. <see cref="User.RoleIds"/> maps
/// via <see cref="ValueConverters.RoleIdsConverter"/> (a JSON column) as of
/// Milestone 10 ("Authorization") - see that converter's doc comment for why
/// a JSON column rather than an owned collection table, and
/// <c>AuthenticationIntegration.md</c> for the identical reasoning already
/// applied to <c>PasswordHistory</c>.
/// </summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "Identity");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasConversion(ValueConverters.UserIdConverter)
            .ValueGeneratedNever();

        builder.Property(u => u.Email)
            .HasConversion(ValueConverters.EmailConverter)
            .HasMaxLength(254)
            .IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.UserName)
            .HasConversion(ValueConverters.UserNameConverter)
            .HasMaxLength(32)
            .IsRequired();
        builder.HasIndex(u => u.UserName).IsUnique();

        builder.Property(u => u.DisplayName)
            .HasConversion(ValueConverters.DisplayNameConverter)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.CreatedAtUtc).IsRequired();

        builder.Property(u => u.RoleIds)
            .HasConversion(ValueConverters.RoleIdsConverter, ValueConverters.RoleIdsComparer)
            .IsRequired();

        builder.Ignore(u => u.DomainEvents);
    }
}
