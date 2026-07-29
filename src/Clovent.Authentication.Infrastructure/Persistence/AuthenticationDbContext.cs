using Clovent.Authentication.Credentials;
using Clovent.Authentication.LoginAttempts;
using Clovent.Authentication.RefreshSessions;
using Clovent.Authentication.Sessions;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Authentication.Infrastructure.Persistence;

/// <summary>
/// EF Core persistence context for the Authentication Domain's four
/// aggregates. All tables live under the <c>Authentication</c> schema (see
/// each <c>IEntityTypeConfiguration</c> in <see cref="Configurations"/>) so
/// this bounded context can share a database with others without name
/// collisions.
/// </summary>
public sealed class AuthenticationDbContext(DbContextOptions<AuthenticationDbContext> options) : DbContext(options)
{
    /// <summary>Session aggregates.</summary>
    public DbSet<Session> Sessions => Set<Session>();

    /// <summary>LoginAttempt aggregates.</summary>
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();

    /// <summary>RefreshSession aggregates.</summary>
    public DbSet<RefreshSession> RefreshSessions => Set<RefreshSession>();

    /// <summary>UserCredentials aggregates.</summary>
    public DbSet<UserCredentials> UserCredentials => Set<UserCredentials>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthenticationDbContext).Assembly);
    }
}
