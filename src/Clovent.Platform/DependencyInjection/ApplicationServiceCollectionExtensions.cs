using Clovent.Platform.Configuration;
using Clovent.Platform.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Platform.DependencyInjection;

/// <summary>
/// Platform Foundation's own Application-layer registration. Every future
/// module is expected to expose an equivalent AddApplication() in its own
/// namespace, following this same naming convention.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers Platform Foundation's Application-layer services: the
    /// validated <see cref="PlatformOptions"/> binding and the module
    /// registry. Call before <see cref="InfrastructureServiceCollectionExtensions.AddInfrastructure"/>
    /// and <see cref="PersistenceServiceCollectionExtensions.AddPersistence"/>,
    /// or simply call <see cref="PlatformServiceCollectionExtensions.AddPlatform"/>
    /// to get all three in order.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The configuration <see cref="PlatformOptions"/> is bound from.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatedOptions<PlatformOptions>(configuration, PlatformOptions.SectionName);
        services.AddModuleRegistry();

        return services;
    }
}
