namespace Clovent.PackageManager.Core.Versioning;

public sealed class VersionComparer
{
    public int Compare(string currentVersion, string newVersion)
    {
        var current = Version.Parse(currentVersion);
        var incoming = Version.Parse(newVersion);

        return current.CompareTo(incoming);
    }

    public bool IsUpgrade(string currentVersion, string newVersion)
        => Compare(currentVersion, newVersion) < 0;

    public bool IsDowngrade(string currentVersion, string newVersion)
        => Compare(currentVersion, newVersion) > 0;

    public bool IsSameVersion(string currentVersion, string newVersion)
        => Compare(currentVersion, newVersion) == 0;
}
