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

    public IReadOnlyList<IModule> RegisteredModules => _modules;

    public bool IsRegistered(string moduleName)
        => _modules.Any(m => string.Equals(m.Name, moduleName, StringComparison.OrdinalIgnoreCase));
}
