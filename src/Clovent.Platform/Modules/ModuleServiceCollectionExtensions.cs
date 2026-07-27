using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Clovent.Platform.Modules;

/// <summary>Registration entry points for the module system.</summary>
public static class ModuleServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="ModuleRegistry"/> singleton, if one isn't
    /// already registered. Called automatically by <see cref="AddModule{TModule}"/>;
    /// most callers won't need this directly.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
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
    /// <typeparam name="TModule">The module to register. Must have a public parameterless constructor.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Passed through to <see cref="IModule.RegisterServices"/> for the module's own use.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
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
