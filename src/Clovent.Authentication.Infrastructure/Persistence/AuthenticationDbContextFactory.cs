using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Clovent.Authentication.Infrastructure.Persistence;

/// <summary>
/// Design-time-only factory the <c>dotnet ef</c> tooling uses to create an
/// <see cref="AuthenticationDbContext"/> for generating/applying migrations
/// from the command line, where no host and no real connection string are
/// available. The connection string here is never used to actually connect -
/// EF Core's migration generation only needs a provider and a model, not a
/// live database. Runtime resolution goes through
/// <c>PersistenceServiceCollectionExtensions.AddPersistence</c> instead,
/// which reads the real connection string from configuration.
/// </summary>
public sealed class AuthenticationDbContextFactory : IDesignTimeDbContextFactory<AuthenticationDbContext>
{
    /// <inheritdoc/>
    public AuthenticationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuthenticationDbContext>();
        optionsBuilder.UseSqlServer("Server=.;Database=Clovent_Authentication_DesignTime;Trusted_Connection=True;TrustServerCertificate=True;");

        return new AuthenticationDbContext(optionsBuilder.Options);
    }
}
