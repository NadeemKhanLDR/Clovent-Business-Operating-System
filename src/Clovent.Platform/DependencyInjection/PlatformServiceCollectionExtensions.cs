using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Platform.DependencyInjection;

/// <summary>
/// Single call for a host to register the whole of Platform Foundation:
/// Application, Infrastructure, and Persistence, in that order.
/// </summary>
public static class PlatformServiceCollectionExtensions
{
    public static IServiceCollection AddPlatform(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication(configuration);
        services.AddInfrastructure(configuration);
        services.AddPersistence(configuration);

        return services;
    }
}
