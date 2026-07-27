using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Platform.DependencyInjection;

/// <summary>
/// Single call for a host to register the whole of Platform Foundation:
/// Application, Infrastructure, and Persistence, in that order.
/// </summary>
public static class PlatformServiceCollectionExtensions
{
    /// <summary>
    /// Registers all of Platform Foundation in the correct order:
    /// <see cref="ApplicationServiceCollectionExtensions.AddApplication"/>,
    /// then <see cref="InfrastructureServiceCollectionExtensions.AddInfrastructure"/>,
    /// then <see cref="PersistenceServiceCollectionExtensions.AddPersistence"/>.
    /// This is what <see cref="Bootstrap.ApplicationBootstrapper.WithPlatform"/>
    /// calls; use it directly if bootstrapping a container outside of
    /// <see cref="Bootstrap.ApplicationBootstrapper"/>.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The configuration Platform Foundation's services are bound from.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddPlatform(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication(configuration);
        services.AddInfrastructure(configuration);
        services.AddPersistence(configuration);

        return services;
    }
}
