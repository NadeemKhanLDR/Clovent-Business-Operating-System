using System.IO.Compression;

namespace Clovent.PackageManager.Core.Builder;

public sealed class PackageBuilder
{
    public void Build(string sourceFolder, string outputFile)
    {
        if (!Directory.Exists(sourceFolder))
            throw new DirectoryNotFoundException(sourceFolder);

        if (File.Exists(outputFile))
            File.Delete(outputFile);

        var zipFile = Path.ChangeExtension(outputFile, ".zip");

        if (File.Exists(zipFile))
            File.Delete(zipFile);

        ZipFile.CreateFromDirectory(
            sourceFolder,
            zipFile,
            CompressionLevel.Optimal,
            false);

        File.Move(zipFile, outputFile);

        Console.WriteLine($"Package created: {outputFile}");
    }
}
