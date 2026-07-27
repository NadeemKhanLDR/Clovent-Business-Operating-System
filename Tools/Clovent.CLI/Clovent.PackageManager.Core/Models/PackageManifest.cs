namespace Clovent.PackageManager.Core.Models;

public sealed class PackageManifest
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string Version { get; set; } = "1.0.0";

    public string Author { get; set; } = "";

    public string Description { get; set; } = "";

    public List<string> Dependencies { get; set; } = new();

    public List<string> Files { get; set; } = new();
}
