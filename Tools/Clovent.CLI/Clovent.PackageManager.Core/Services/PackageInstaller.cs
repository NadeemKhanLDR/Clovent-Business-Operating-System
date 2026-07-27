using Clovent.PackageManager.Abstractions;
using Clovent.PackageManager.Core.Backup;
using Clovent.PackageManager.Core.Dependencies;
using Clovent.PackageManager.Core.Packages;
using Clovent.PackageManager.Core.Readers;
using Clovent.PackageManager.Core.Registry;
using Clovent.PackageManager.Core.Versioning;

namespace Clovent.PackageManager.Core.Services;

public sealed class PackageInstaller : IPackageInstaller
{
    public async Task InstallAsync(string packageFile)
    {
        var packageName = Path.GetFileNameWithoutExtension(packageFile);

        var destination = Path.Combine(
            Environment.CurrentDirectory,
            "InstalledPackages",
            packageName);

        var backup = new BackupService();

        try
        {
            var reader = new PackageReader();
            reader.Extract(packageFile, destination);

            var manifest = new ManifestReader().Read(destination);

            // Validate dependencies
            new DependencyResolver().Validate(manifest);

            var registry = new PackageRegistry();
            var installed = registry.Load();

            var existing = installed.FirstOrDefault(x => x.Id == manifest.Id);

            if (existing != null)
            {
                var comparer = new VersionComparer();

                if (comparer.IsSameVersion(existing.Version, manifest.Version))
                {
                    Console.WriteLine("Package already installed.");
                    return;
                }

                if (comparer.IsDowngrade(existing.Version, manifest.Version))
                {
                    throw new InvalidOperationException(
                        $"Downgrade blocked: {existing.Version} -> {manifest.Version}");
                }

                Console.WriteLine($"Upgrade: {existing.Version} -> {manifest.Version}");
                installed.Remove(existing);
            }

            installed.Add(new InstalledPackage
            {
                Id = manifest.Id,
                Version = manifest.Version,
                InstalledOn = DateTime.UtcNow
            });

            registry.Save(installed);

            Console.WriteLine($"Installed: {manifest.Name} {manifest.Version}");
        }
        catch
        {
            backup.Rollback(destination);
            throw;
        }

        await Task.CompletedTask;
    }

    public async Task UninstallAsync(string packageId)
    {
        var folder = Path.Combine(
            Environment.CurrentDirectory,
            "InstalledPackages",
            packageId);

        if (Directory.Exists(folder))
            Directory.Delete(folder, true);

        var registry = new PackageRegistry();
        var packages = registry.Load();

        packages.RemoveAll(x => x.Id == packageId);

        registry.Save(packages);

        Console.WriteLine($"Uninstalled: {packageId}");

        await Task.CompletedTask;
    }

    public async Task VerifyAsync()
    {
        var registry = new PackageRegistry();

        Console.WriteLine($"Installed packages: {registry.Load().Count}");

        await Task.CompletedTask;
    }
}
