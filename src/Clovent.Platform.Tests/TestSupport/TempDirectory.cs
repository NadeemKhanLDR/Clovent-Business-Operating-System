namespace Clovent.Platform.Tests.TestSupport;

/// <summary>
/// Creates an isolated temp directory for a single test (used for
/// appsettings.json fixtures) and deletes it on dispose.
/// </summary>
public sealed class TempDirectory : IDisposable
{
    public string Path { get; }

    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "clovent-platform-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(Path);
    }

    public void WriteFile(string fileName, string content)
        => File.WriteAllText(System.IO.Path.Combine(Path, fileName), content);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
