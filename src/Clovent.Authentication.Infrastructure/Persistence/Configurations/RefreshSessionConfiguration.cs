using Clovent.Authentication.RefreshSessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Authentication.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="RefreshSession"/>.</summary>
internal sealed class RefreshSessionConfiguration : IEntityTypeConfiguration<RefreshSession>
{
    public void Configure(EntityTypeBuilder<RefreshSession> builder)
    {
        builder.ToTable("RefreshSessions", "Authentication");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(ValueConverters.RefreshSessionIdConverter)
            .ValueGeneratedNever();

        builder.Property(r => r.SessionId)
            .HasConversion(ValueConverters.SessionIdConverter)
            .IsRequired();
        builder.HasIndex(r => r.SessionId);

        builder.Property(r => r.IssuedAtUtc).IsRequired();
        builder.Property(r => r.ExpiresAtUtc).IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(r => new { r.SessionId, r.Status });

        builder.Ignore(r => r.DomainEvents);
    }
}
