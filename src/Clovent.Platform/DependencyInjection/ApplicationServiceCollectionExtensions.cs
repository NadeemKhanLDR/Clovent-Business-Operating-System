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
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatedOptions<PlatformOptions>(configuration, PlatformOptions.SectionName);
        services.AddModuleRegistry();

        return services;
    }
}
