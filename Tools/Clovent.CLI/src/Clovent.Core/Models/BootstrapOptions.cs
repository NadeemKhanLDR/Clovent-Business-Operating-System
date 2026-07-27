namespace Clovent.Core.Models;

public sealed class BootstrapOptions
{
    public string? RootPath { get; init; }

    public bool CreateMissingFolders { get; init; }

    public bool CreateMissingFiles { get; init; }
}
