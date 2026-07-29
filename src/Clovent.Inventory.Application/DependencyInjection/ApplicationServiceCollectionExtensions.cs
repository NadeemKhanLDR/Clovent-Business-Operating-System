using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Inventory.Application.DependencyInjection;

/// <summary>Inventory's own Application-layer registration, mirroring <c>Clovent.Catalog.Application</c>'s convention.</summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers MediatR, scanning this assembly for every <c>IRequestHandler</c> defined here (WarehouseStocks, Transactions, Adjustments, Transfers).</summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Accepted for signature consistency with the AddApplication()/AddInfrastructure()/AddPersistence() convention; not read today.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly));

        return services;
    }
}
