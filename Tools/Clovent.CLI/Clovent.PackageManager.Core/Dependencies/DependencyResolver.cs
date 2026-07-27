using Clovent.PackageManager.Core.Models;
using Clovent.PackageManager.Core.Registry;

namespace Clovent.PackageManager.Core.Dependencies;

public sealed class DependencyResolver
{
    public void Validate(PackageManifest manifest)
    {
        var registry = new PackageRegistry();
        var installed = registry.Load();

        foreach (var dependency in manifest.Dependencies)
        {
            if (!installed.Any(p => p.Id.Equals(dependency, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"Missing dependency: {dependency}");
            }
        }
    }
}
