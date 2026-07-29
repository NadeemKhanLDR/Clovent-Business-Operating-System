using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Clovent.Identity.Infrastructure.Persistence;

/// <summary>Design-time-only factory the <c>dotnet ef</c> tooling uses to create an <see cref="IdentityDbContext"/> - see the identical <c>AuthenticationDbContextFactory</c> for the full reasoning.</summary>
public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    /// <inheritdoc/>
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseSqlServer("Server=.;Database=Clovent_Identity_DesignTime;Trusted_Connection=True;TrustServerCertificate=True;");

        return new IdentityDbContext(optionsBuilder.Options);
    }
}
