namespace Clovent.PackageManager.Core.Registry;

public sealed class InstalledPackage
{
    public string Id { get; set; } = "";

    public string Version { get; set; } = "";

    public DateTime InstalledOn { get; set; } = DateTime.UtcNow;
}
