namespace Clovent.CLI.Commands.Shared;

public static class ValidationHelper
{
    public static bool HasValue(string? value)
        => !string.IsNullOrWhiteSpace(value);

    public static bool DirectoryExists(string path)
        => Directory.Exists(path);

    public static bool FileExists(string path)
        => File.Exists(path);
}
