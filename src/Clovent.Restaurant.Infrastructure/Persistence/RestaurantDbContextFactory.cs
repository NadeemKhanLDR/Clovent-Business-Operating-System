using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Clovent.Restaurant.Infrastructure.Persistence;

/// <summary>Design-time-only factory the <c>dotnet ef</c> tooling uses to create a <see cref="RestaurantDbContext"/> - see the identical <c>CatalogDbContextFactory</c> for the full reasoning.</summary>
public sealed class RestaurantDbContextFactory : IDesignTimeDbContextFactory<RestaurantDbContext>
{
    /// <inheritdoc/>
    public RestaurantDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RestaurantDbContext>();
        optionsBuilder.UseSqlServer("Server=.;Database=Clovent_Restaurant_DesignTime;Trusted_Connection=True;TrustServerCertificate=True;");

        return new RestaurantDbContext(optionsBuilder.Options);
    }
}
