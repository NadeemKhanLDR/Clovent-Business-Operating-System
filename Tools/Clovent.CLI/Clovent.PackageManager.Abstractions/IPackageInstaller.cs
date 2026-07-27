namespace Clovent.PackageManager.Abstractions;

public interface IPackageInstaller
{
    Task InstallAsync(string packageFile);

    Task UninstallAsync(string packageId);

    Task VerifyAsync();
}
