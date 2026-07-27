using System.Text.Json;
using Clovent.PackageManager.Core.Models;

namespace Clovent.PackageManager.Core.Readers;

public sealed class ManifestReader
{
    public PackageManifest Read(string packageFolder)
    {
        var manifestPath = Path.Combine(packageFolder, "package.json");

        if (!File.Exists(manifestPath))
            throw new FileNotFoundException("package.json not found.");

        var json = File.ReadAllText(manifestPath);

        var manifest = JsonSerializer.Deserialize<PackageManifest>(json);

        if (manifest is null)
            throw new Exception("Invalid package manifest.");

        if (string.IsNullOrWhiteSpace(manifest.Id))
            throw new Exception("Package Id is required.");

        if (string.IsNullOrWhiteSpace(manifest.Version))
            throw new Exception("Package Version is required.");

        return manifest;
    }
}
