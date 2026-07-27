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
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}
