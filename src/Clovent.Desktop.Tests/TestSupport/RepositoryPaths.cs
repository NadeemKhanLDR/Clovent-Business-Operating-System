using System.Reflection;

namespace Clovent.Desktop.Tests.TestSupport;

/// <summary>
/// Resolves repository source paths and built tool assemblies both for standard in-tree builds
/// and off-repository builds (for example MSBuild <c>UseArtifactsOutput=true</c> on another drive).
/// </summary>
internal static class RepositoryPaths
{
    public const string RepoRootEnvironmentVariable = "CBOS_REPO_ROOT";
    public const string ProvisionerPathEnvironmentVariable = "CBOS_PROVISIONER_PATH";

    private const string SolutionFileName = "Clovent.BusinessOperatingSystem.slnx";
    private const string ProvisionerAssemblyFileName = "Clovent.Installer.Provisioner.dll";

    public static string FindRepositoryRoot()
    {
        var envRoot = Environment.GetEnvironmentVariable(RepoRootEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(envRoot))
        {
            var fullEnvRoot = Path.GetFullPath(envRoot.Trim());
            if (!IsValidRepositoryRoot(fullEnvRoot))
            {
                throw new DirectoryNotFoundException(
                    $"{RepoRootEnvironmentVariable} is set to '{fullEnvRoot}', which does not contain '{SolutionFileName}' and 'src/Clovent.Desktop/Clovent.Desktop.csproj'.");
            }

            return fullEnvRoot;
        }

        if (TryWalkUpToRepositoryRoot(AppContext.BaseDirectory, out var walkedRoot))
        {
            return walkedRoot;
        }

        var metadataRoot = GetAssemblyMetadata("CbosRepoRoot");
        if (!string.IsNullOrWhiteSpace(metadataRoot))
        {
            var fullMetadataRoot = Path.GetFullPath(metadataRoot.Trim());
            if (IsValidRepositoryRoot(fullMetadataRoot))
            {
                return fullMetadataRoot;
            }
        }

        throw new DirectoryNotFoundException(
            $"Unable to locate repository root containing '{SolutionFileName}'. " +
            $"Set {RepoRootEnvironmentVariable} or rebuild {typeof(RepositoryPaths).Assembly.GetName().Name} with CbosRepoRoot metadata.");
    }

    public static string GetDesktopProjectDirectory()
    {
        var desktopDir = Path.Combine(FindRepositoryRoot(), "src", "Clovent.Desktop");
        if (!Directory.Exists(desktopDir))
        {
            throw new DirectoryNotFoundException($"Expected Desktop project directory at '{desktopDir}'.");
        }

        return desktopDir;
    }

    public static string GetDesktopSourceFile(params string[] relativeSegments)
    {
        var path = Path.Combine([GetDesktopProjectDirectory(), .. relativeSegments]);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Expected Desktop source file at '{path}'.", path);
        }

        return path;
    }

    public static string ResolveProvisionerAssembly()
    {
        var probedLocations = new List<string>();

        var explicitPath = Environment.GetEnvironmentVariable(ProvisionerPathEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            var fullExplicit = Path.GetFullPath(explicitPath.Trim());
            if (!File.Exists(fullExplicit))
            {
                throw new FileNotFoundException(
                    $"{ProvisionerPathEnvironmentVariable} is set to '{fullExplicit}', but the file does not exist.",
                    fullExplicit);
            }

            if (!string.Equals(Path.GetFileName(fullExplicit), ProvisionerAssemblyFileName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"{ProvisionerPathEnvironmentVariable} must point to '{ProvisionerAssemblyFileName}', not '{fullExplicit}'.");
            }

            return fullExplicit;
        }

        var preferredConfig = GetAssemblyMetadata("CbosBuildConfiguration") ?? "Debug";

        foreach (var artifactsBinRoot in EnumerateCandidateArtifactsRoots())
        {
            var provisionerArtifactsBin = Path.Combine(artifactsBinRoot, "bin", "Clovent.Installer.Provisioner");
            probedLocations.Add(provisionerArtifactsBin);
            if (TryFindProvisionerInDirectory(provisionerArtifactsBin, preferredConfig, out var found))
            {
                return found;
            }
        }

        var repoRoot = FindRepositoryRoot();
        var orderedConfigs = string.Equals(preferredConfig, "Release", StringComparison.OrdinalIgnoreCase)
            ? new[] { "Release", "Debug" }
            : new[] { "Debug", "Release" };

        foreach (var config in orderedConfigs)
        {
            var inTreeCandidate = Path.Combine(
                repoRoot,
                "tools",
                "Clovent.Installer.Provisioner",
                "bin",
                config,
                "net10.0-windows",
                "win-x64",
                ProvisionerAssemblyFileName);
            probedLocations.Add(inTreeCandidate);
            if (File.Exists(inTreeCandidate))
            {
                return inTreeCandidate;
            }
        }

        throw new FileNotFoundException(
            $"Unable to locate compiled '{ProvisionerAssemblyFileName}'. Build tools/Clovent.Installer.Provisioner/Clovent.Installer.Provisioner.csproj " +
            $"or set {ProvisionerPathEnvironmentVariable}. Probed: {string.Join("; ", probedLocations.Distinct(StringComparer.OrdinalIgnoreCase))}");
    }

    private static IEnumerable<string> EnumerateCandidateArtifactsRoots()
    {
        var metadataArtifacts = GetAssemblyMetadata("CbosArtifactsPath");
        if (!string.IsNullOrWhiteSpace(metadataArtifacts))
        {
            yield return Path.GetFullPath(metadataArtifacts.Trim());
        }

        // Standard artifacts layout places test binaries under <ArtifactsPath>\bin\Clovent.Desktop.Tests\<config>.
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (int depth = 0; depth < 4 && current is not null; depth++, current = current.Parent)
        {
            if (string.Equals(current.Name, "bin", StringComparison.OrdinalIgnoreCase) && current.Parent is not null)
            {
                yield return current.Parent.FullName;
            }
        }
    }

    private static bool TryFindProvisionerInDirectory(string rootDir, string preferredConfig, out string assemblyPath)
    {
        assemblyPath = string.Empty;
        if (!Directory.Exists(rootDir))
        {
            return false;
        }

        var matches = Directory.GetFiles(rootDir, ProvisionerAssemblyFileName, SearchOption.AllDirectories)
            .OrderByDescending(p => p.Contains(preferredConfig, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(File.GetLastWriteTimeUtc)
            .ToArray();

        if (matches.Length == 0)
        {
            return false;
        }

        assemblyPath = matches[0];
        return true;
    }

    private static bool TryWalkUpToRepositoryRoot(string startDirectory, out string repoRoot)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            if (IsValidRepositoryRoot(directory.FullName))
            {
                repoRoot = directory.FullName;
                return true;
            }

            directory = directory.Parent;
        }

        repoRoot = string.Empty;
        return false;
    }

    private static bool IsValidRepositoryRoot(string candidatePath) =>
        Directory.Exists(candidatePath) &&
        File.Exists(Path.Combine(candidatePath, SolutionFileName)) &&
        File.Exists(Path.Combine(candidatePath, "src", "Clovent.Desktop", "Clovent.Desktop.csproj"));

    private static string? GetAssemblyMetadata(string key) =>
        typeof(RepositoryPaths).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase))
            ?.Value;
}
