namespace Clovent.Core.Services;

using Clovent.Core.Interfaces;
using Clovent.Core.Results;

public sealed class DoctorService : IDoctorService
{
    public DoctorResult Diagnose(string rootPath)
    {
        var result = new DoctorResult();

        // 1. Check directory existence
        var dirExists = Directory.Exists(rootPath);
        result.Checks.Add(new DoctorCheckItem
        {
            Name = "Workspace Root Directory",
            IsPassed = dirExists,
            Details = dirExists ? $"Found at {rootPath}" : $"Directory does not exist at {rootPath}"
        });

        // 2. Check .NET runtime version
        var dotnetVersion = Environment.Version.ToString();
        result.Checks.Add(new DoctorCheckItem
        {
            Name = ".NET Runtime Version",
            IsPassed = true,
            Details = $"Running on .NET Runtime v{dotnetVersion}"
        });

        // 3. Check Obsidian Vault Folders
        var obsidianDir = Path.Combine(rootPath, ".obsidian");
        var hasObsidian = Directory.Exists(obsidianDir);
        result.Checks.Add(new DoctorCheckItem
        {
            Name = "Obsidian Documentation Vault",
            IsPassed = hasObsidian,
            Details = hasObsidian ? "Obsidian vault configured (.obsidian directory exists)" : "Obsidian vault directory not found"
        });

        return result;
    }
}
