using Clovent.Authentication.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Authentication.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="UserCredentials"/>.</summary>
internal sealed class UserCredentialsConfiguration : IEntityTypeConfiguration<UserCredentials>
{
    public void Configure(EntityTypeBuilder<UserCredentials> builder)
    {
        builder.ToTable("UserCredentials", "Authentication");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(ValueConverters.UserCredentialsIdConverter)
            .ValueGeneratedNever();

        builder.Property(c => c.UserId)
            .HasConversion(ValueConverters.UserIdConverter)
            .IsRequired();
        builder.HasIndex(c => c.UserId).IsUnique();

        builder.Property(c => c.PasswordHash)
            .HasConversion(ValueConverters.PasswordHashConverter)
            .HasMaxLength(512);

        builder.Property(c => c.PinHash)
            .HasConversion(ValueConverters.PinHashConverter)
            .HasMaxLength(512);

        builder.Property(c => c.SecurityStamp)
            .HasConversion(ValueConverters.SecurityStampConverter)
            .HasMaxLength(64)
            .IsRequired();

        // No explicit HasColumnType: SQL Server already defaults an unbounded
        // string property to nvarchar(max), and a hardcoded "nvarchar(max)"
        // literal breaks provider-agnostic tooling (e.g. SQLite in tests,
        // which doesn't understand SQL Server's "max" size token).
        builder.Property(c => c.PasswordHistory)
            .HasConversion(ValueConverters.PasswordHistoryConverter)
            .IsRequired();

        builder.Property(c => c.FailedAttempts)
            .HasConversion(ValueConverters.FailedAttemptsConverter)
            .IsRequired();

        builder.Ignore(c => c.DomainEvents);
    }
}
