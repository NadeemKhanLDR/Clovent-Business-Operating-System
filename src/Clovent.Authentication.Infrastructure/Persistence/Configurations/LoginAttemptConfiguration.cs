using Clovent.Authentication.LoginAttempts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Authentication.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="LoginAttempt"/>.</summary>
internal sealed class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        builder.ToTable("LoginAttempts", "Authentication");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(ValueConverters.LoginAttemptIdConverter)
            .ValueGeneratedNever();

        builder.Property(a => a.AttemptedIdentifier)
            .HasMaxLength(320)
            .IsRequired();
        builder.HasIndex(a => a.AttemptedIdentifier);

        builder.Property(a => a.UserId)
            .HasConversion(ValueConverters.NullableUserIdConverter);
        builder.HasIndex(a => a.UserId);

        builder.Property(a => a.Outcome)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(a => a.IpAddress)
            .HasConversion(ValueConverters.IpAddressConverter)
            .HasMaxLength(45);

        builder.Property(a => a.OccurredAtUtc).IsRequired();

        builder.Ignore(a => a.IsFailure);
        builder.Ignore(a => a.DomainEvents);
    }
}
