using System.Reflection;
using Clovent.Platform.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Modules;

/// <summary>
/// Registers every module in <see cref="DesktopModuleCatalog.ModuleTypes"/>
/// against <see cref="ModuleServiceCollectionExtensions.AddModule{TModule}"/>.
/// That method is generic (a compile-time type parameter), so a runtime list
/// of module <see cref="Type"/>s needs reflection to invoke it once per
/// entry - this class is that one piece of reflection, kept out of
/// <see cref="Program"/> so the boot sequence stays readable.
/// </summary>
public static class DesktopModuleLoader
{
    private static readonly MethodInfo AddModuleMethod = typeof(ModuleServiceCollectionExtensions)
        .GetMethod(nameof(ModuleServiceCollectionExtensions.AddModule))!;

    /// <summary>Registers every module type in <paramref name="moduleTypes"/> via <see cref="ModuleServiceCollectionExtensions.AddModule{TModule}"/>.</summary>
    /// <exception cref="ArgumentException">A type in <paramref name="moduleTypes"/> does not implement <see cref="IModule"/> or lacks a public parameterless constructor.</exception>
    public static IServiceCollection LoadModules(this IServiceCollection services, IConfiguration configuration, IEnumerable<Type> moduleTypes)
    {
        foreach (var moduleType in moduleTypes)
        {
            if (!typeof(IModule).IsAssignableFrom(moduleType))
            {
                throw new ArgumentException($"'{moduleType.FullName}' does not implement {nameof(IModule)}.", nameof(moduleTypes));
            }

            if (moduleType.GetConstructor(Type.EmptyTypes) is null)
            {
                throw new ArgumentException($"'{moduleType.FullName}' has no public parameterless constructor.", nameof(moduleTypes));
            }

            AddModuleMethod.MakeGenericMethod(moduleType).Invoke(null, [services, configuration]);
        }

        return services;
    }
}
