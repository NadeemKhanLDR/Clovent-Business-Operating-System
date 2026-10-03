using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Clovent.Catalog.Infrastructure.Persistence;

/// <summary>Design-time-only factory the <c>dotnet ef</c> tooling uses to create a <see cref="CatalogDbContext"/> - see the identical <c>MasterDataDbContextFactory</c> for the full reasoning.</summary>
public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    /// <inheritdoc/>
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=.;Database=Clovent_BusinessOperatingSystem;Trusted_Connection=True;TrustServerCertificate=True;",
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Catalog"));

        return new CatalogDbContext(optionsBuilder.Options);
    }
}
