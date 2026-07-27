using System.Text.Json;

namespace Clovent.PackageManager.Core.Registry;

public sealed class PackageRegistry
{
    private readonly string _registryFile =
        Path.Combine(Environment.CurrentDirectory, "installed.json");

    public List<InstalledPackage> Load()
    {
        if (!File.Exists(_registryFile))
            return new();

        return JsonSerializer.Deserialize<List<InstalledPackage>>(
            File.ReadAllText(_registryFile)) ?? new();
    }

    public void Save(List<InstalledPackage> packages)
    {
        File.WriteAllText(
            _registryFile,
            JsonSerializer.Serialize(
                packages,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
    }
}
