using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.MasterData.Application.DependencyInjection;

/// <summary>
/// MasterData's own Application-layer registration, mirroring
/// <c>Clovent.Authentication.Application</c>'s and
/// <c>Clovent.Identity.Application</c>'s convention.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers MediatR, scanning this assembly for every <c>IRequestHandler</c> defined here (Departments, Warehouses, Terminals, FiscalYears, Currencies, Languages, TimeZones, Settings).</summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Accepted for signature consistency with the AddApplication()/AddInfrastructure()/AddPersistence() convention; not read today.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly));

        return services;
    }
}
