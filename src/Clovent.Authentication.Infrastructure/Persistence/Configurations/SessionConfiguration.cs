using Clovent.Authentication.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Authentication.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Session"/>.</summary>
internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions", "Authentication");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(ValueConverters.SessionIdConverter)
            .ValueGeneratedNever();

        builder.Property(s => s.UserId)
            .HasConversion(ValueConverters.UserIdConverter)
            .IsRequired();
        builder.HasIndex(s => s.UserId);

        builder.Property(s => s.IpAddress)
            .HasConversion(ValueConverters.IpAddressConverter)
            .HasMaxLength(45);

        builder.Property(s => s.IdleTimeout)
            .HasConversion(ValueConverters.TimeSpanTicksConverter)
            .IsRequired();

        builder.Property(s => s.StartedAtUtc).IsRequired();
        builder.Property(s => s.LastActivityAtUtc).IsRequired();
        builder.Property(s => s.ExpiresAtUtc).IsRequired();

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(s => new { s.UserId, s.Status });

        builder.Ignore(s => s.DomainEvents);
    }
}
