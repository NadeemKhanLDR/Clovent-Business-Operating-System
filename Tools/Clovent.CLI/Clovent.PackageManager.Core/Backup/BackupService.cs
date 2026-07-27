namespace Clovent.PackageManager.Core.Backup;

public sealed class BackupService
{
    public void Rollback(string folder)
    {
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, true);
            Console.WriteLine("Rollback completed.");
        }
    }
}
