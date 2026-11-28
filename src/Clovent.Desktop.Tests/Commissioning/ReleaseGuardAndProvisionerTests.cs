using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace Clovent.Desktop.Tests.Commissioning;

public sealed class ReleaseGuardAndProvisionerTests
{
    private static string GetRepoRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Fact]
    public void ProvisionerCli_UpgradeFlag_ParsesAndRejectsVerifyOnlyCombination()
    {
        var repoRoot = GetRepoRoot();
        var debugDll = Path.Combine(repoRoot, "tools", "Clovent.Installer.Provisioner", "bin", "Debug", "net10.0-windows", "win-x64", "Clovent.Installer.Provisioner.dll");
        var releaseDll = Path.Combine(repoRoot, "tools", "Clovent.Installer.Provisioner", "bin", "Release", "net10.0-windows", "win-x64", "Clovent.Installer.Provisioner.dll");
        var targetDll = File.Exists(debugDll) ? debugDll : releaseDll;
        Assert.True(File.Exists(targetDll), $"Expected compiled provisioner assembly at '{debugDll}' or '{releaseDll}'.");

        var asm = Assembly.LoadFrom(targetDll);
        var programType = asm.GetType("Clovent.Installer.Provisioner.Program", throwOnError: true)!;
        var tryParse = programType.GetMethod("TryParseArguments", BindingFlags.Public | BindingFlags.Static)!;

        // 1. Valid --upgrade
        object?[] validArgs = [new[] { "--server", ".", "--upgrade" }, null, null];
        var ok = (bool)tryParse.Invoke(null, validArgs)!;
        Assert.True(ok);
        Assert.NotNull(validArgs[1]);
        var upgradeProp = validArgs[1]!.GetType().GetProperty("Upgrade")!;
        Assert.True((bool)upgradeProp.GetValue(validArgs[1])!);

        // 2. Invalid --upgrade --verify-only
        object?[] conflictArgs = [new[] { "--upgrade", "--verify-only" }, null, null];
        var conflictOk = (bool)tryParse.Invoke(null, conflictArgs)!;
        Assert.False(conflictOk);
    }

    [Fact]
    public void ReleaseGuard_ScanReleasePackage_PassesCleanPackage_AndDetectsViolations()
    {
        var repoRoot = GetRepoRoot();
        var scriptPath = Path.Combine(repoRoot, "Tools", "ReleaseGuard", "ScanReleasePackage.ps1");
        Assert.True(File.Exists(scriptPath), $"ReleaseGuard script not found at '{scriptPath}'.");

        var tempRoot = Path.Combine(Path.GetTempPath(), $"cbos-releaseguard-{Guid.NewGuid():N}");
        var cleanDir = Path.Combine(tempRoot, "clean");
        var dirtyPdbDir = Path.Combine(tempRoot, "dirty-pdb");
        var dirtyDevJsonDir = Path.Combine(tempRoot, "dirty-devjson");
        var dirtySecretDir = Path.Combine(tempRoot, "dirty-secret");
        var dirtyBakDir = Path.Combine(tempRoot, "dirty-bak");

        try
        {
            Directory.CreateDirectory(cleanDir);
            File.WriteAllText(Path.Combine(cleanDir, "appsettings.json"), "{\"Logging\":{\"LogLevel\":{\"Default\":\"Information\"}}}");

            Directory.CreateDirectory(dirtyPdbDir);
            File.WriteAllText(Path.Combine(dirtyPdbDir, "Clovent.Desktop.pdb"), "pdb");

            Directory.CreateDirectory(dirtyDevJsonDir);
            File.WriteAllText(Path.Combine(dirtyDevJsonDir, "appsettings.Development.json"), "{}");

            Directory.CreateDirectory(dirtySecretDir);
            File.WriteAllText(Path.Combine(dirtySecretDir, "config.json"), "{\"Password\": \"SuperSecret123\"}");

            Directory.CreateDirectory(dirtyBakDir);
            File.WriteAllText(Path.Combine(dirtyBakDir, "customer.bak"), "bak");

            Assert.Equal(0, RunScanScript(scriptPath, cleanDir));
            Assert.Equal(1, RunScanScript(scriptPath, dirtyPdbDir));
            Assert.Equal(1, RunScanScript(scriptPath, dirtyDevJsonDir));
            Assert.Equal(1, RunScanScript(scriptPath, dirtySecretDir));
            Assert.Equal(1, RunScanScript(scriptPath, dirtyBakDir));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void SignReleaseArtifacts_RequireSigningWithoutCert_FailsWithExitCode1()
    {
        var repoRoot = GetRepoRoot();
        var signScript = Path.Combine(repoRoot, "Tools", "ReleaseGuard", "SignReleaseArtifacts.ps1");
        Assert.True(File.Exists(signScript), $"Signing script not found at '{signScript}'.");

        var dummyExe = Path.Combine(Path.GetTempPath(), $"cbos-sign-test-{Guid.NewGuid():N}.exe");
        File.WriteAllText(dummyExe, "dummy");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{signScript}\" -TargetPaths \"{dummyExe}\" -RequireSigning",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.Environment["CBOS_SIGN_PFX_PATH"] = string.Empty;
            psi.Environment["CBOS_SIGN_THUMBPRINT"] = string.Empty;

            using var proc = Process.Start(psi)!;
            proc.WaitForExit(15000);
            Assert.Equal(1, proc.ExitCode);
        }
        finally
        {
            if (File.Exists(dummyExe)) File.Delete(dummyExe);
        }
    }

    private static int RunScanScript(string scriptPath, string targetDir)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -ReleaseDir \"{targetDir}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi)!;
        proc.WaitForExit(15000);
        return proc.ExitCode;
    }
}
