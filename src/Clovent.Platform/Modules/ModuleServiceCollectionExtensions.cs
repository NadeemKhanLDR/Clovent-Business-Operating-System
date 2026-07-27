using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Clovent.Platform.Modules;

public static class ModuleServiceCollectionExtensions
{
    public static IServiceCollection AddModuleRegistry(this IServiceCollection services)
    {
        services.TryAddSingleton<ModuleRegistry>();
        return services;
    }

    /// <summary>
    /// Registers a module and lets it wire up its own services. Adding a
    /// new module to a host is always exactly this one call - no switch
    /// statement, no manually-maintained list of module types.
    /// </summary>
    public static IServiceCollection AddModule<TModule>(this IServiceCollection services, IConfiguration configuration)
        where TModule : IModule, new()
    {
        services.AddModuleRegistry();

        var module = new TModule();
        services.AddSingleton<IModule>(module);
        module.RegisterServices(services, configuration);

        return services;
    }
}
