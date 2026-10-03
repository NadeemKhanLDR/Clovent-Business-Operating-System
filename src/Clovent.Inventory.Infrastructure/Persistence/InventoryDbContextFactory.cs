using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Clovent.Inventory.Infrastructure.Persistence;

/// <summary>Design-time-only factory the <c>dotnet ef</c> tooling uses to create an <see cref="InventoryDbContext"/> - see the identical <c>CatalogDbContextFactory</c> for the full reasoning.</summary>
public sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    /// <inheritdoc/>
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=.;Database=Clovent_BusinessOperatingSystem;Trusted_Connection=True;TrustServerCertificate=True;",
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Inventory"));

        return new InventoryDbContext(optionsBuilder.Options);
    }
}
