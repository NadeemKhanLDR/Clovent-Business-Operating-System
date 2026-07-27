namespace Clovent.Platform.Modules;

/// <summary>
/// Read-only view over every module registered via
/// <see cref="ModuleServiceCollectionExtensions.AddModule{TModule}"/>.
/// Populated purely by resolving IEnumerable&lt;IModule&gt; from the
/// container - there is no separate list to keep in sync.
/// </summary>
public sealed class ModuleRegistry
{
    private readonly IReadOnlyList<IModule> _modules;

    /// <summary>
    /// Constructed by DI, not typically called directly. Resolving this type
    /// pulls every <see cref="IModule"/> registered so far via
    /// <see cref="ModuleServiceCollectionExtensions.AddModule{TModule}"/>.
    /// </summary>
    /// <param name="modules">Every module registered in the container.</param>
    /// <exception cref="InvalidOperationException">Two or more modules share the same <see cref="IModule.Name"/> (case-insensitive).</exception>
    public ModuleRegistry(IEnumerable<IModule> modules)
    {
        var list = modules.ToList();

        var duplicate = list
            .GroupBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Module '{duplicate.Key}' is registered more than once.");
        }

        _modules = list;
    }

    /// <summary>Every module currently registered in the container, in registration order.</summary>
    public IReadOnlyList<IModule> RegisteredModules => _modules;

    /// <summary>Whether a module with the given <see cref="IModule.Name"/> (case-insensitive) is registered.</summary>
    /// <param name="moduleName">The module name to look up.</param>
    public bool IsRegistered(string moduleName)
        => _modules.Any(m => string.Equals(m.Name, moduleName, StringComparison.OrdinalIgnoreCase));
}
