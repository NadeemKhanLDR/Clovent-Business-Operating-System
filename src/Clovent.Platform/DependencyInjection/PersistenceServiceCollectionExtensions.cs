using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Platform.DependencyInjection;

/// <summary>
/// Platform Foundation's own Persistence-layer registration. Platform
/// Foundation has no database of its own, so this is an extensibility
/// point today - future modules register their own
/// IPersistenceInitializer/DbContext from within their own
/// AddPersistence(), following this same naming convention.
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Extensibility point for Persistence-layer registration. Platform
    /// Foundation has no persistence of its own, so this currently does
    /// nothing - it exists so the AddApplication()/AddInfrastructure()/
    /// AddPersistence() convention is complete from day one, and so future
    /// modules have a documented place to register their own <c>DbContext</c>
    /// and <see cref="Bootstrap.IPersistenceInitializer"/> implementations.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Configuration available to persistence registrations (e.g. connection strings).</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}
