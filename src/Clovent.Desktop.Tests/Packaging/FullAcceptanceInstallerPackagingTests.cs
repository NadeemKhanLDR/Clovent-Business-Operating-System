using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Clovent.Desktop.Tests.TestSupport;
using Xunit;

namespace Clovent.Desktop.Tests.Packaging;

/// <summary>
/// Packaging, isolation, and provisioning safety checks for the full internal acceptance installer
/// (cbos-acceptance-setup.iss) and its associated tooling (Launch-Acceptance.cmd, Clovent.Installer.Provisioner).
/// </summary>
public sealed class FullAcceptanceInstallerPackagingTests
{
    private readonly string _repoRoot;
    private readonly string _acceptanceIssPath;
    private readonly string _acceptanceLaunchCmdPath;
    private readonly string _acceptanceIssContent;
    private readonly string _effectiveDirectives;

    public FullAcceptanceInstallerPackagingTests()
    {
        _repoRoot = RepositoryPaths.FindRepositoryRoot();
        _acceptanceIssPath = Path.Combine(_repoRoot, "Tools", "installer", "cbos-acceptance-setup.iss");
        _acceptanceLaunchCmdPath = Path.Combine(_repoRoot, "Tools", "installer", "Launch-Acceptance.cmd");

        Assert.True(File.Exists(_acceptanceIssPath), $"Expected acceptance installer script at '{_acceptanceIssPath}'.");
        Assert.True(File.Exists(_acceptanceLaunchCmdPath), $"Expected acceptance launcher script at '{_acceptanceLaunchCmdPath}'.");

        _acceptanceIssContent = File.ReadAllText(_acceptanceIssPath);
        _effectiveDirectives = string.Join(
            Environment.NewLine,
            _acceptanceIssContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                .Where(line => !line.TrimStart().StartsWith(";")));
    }

    [Fact]
    public void AcceptanceSetupScript_HasDistinctAppIdAndInstallationDirectory_PreventingOperationalCollision()
    {
        const string productionAppId = "{{C107E47D-CB05-47F1-9BD7-A107CB052026}}";
        const string productionDefaultDir = @"{autopf}\Clovent\Business Operating System";

        var appIdMatch = Regex.Match(_acceptanceIssContent, @"#define\s+MyAppId\s+""([^""]+)""");
        Assert.True(appIdMatch.Success, "MyAppId must be defined in cbos-acceptance-setup.iss.");
        var acceptanceAppId = appIdMatch.Groups[1].Value.Trim();

        Assert.NotEqual(productionAppId, acceptanceAppId);
        Assert.Contains("FULL", acceptanceAppId, StringComparison.OrdinalIgnoreCase);

        var dirMatch = Regex.Match(_acceptanceIssContent, @"DefaultDirName=([^\r\n]+)");
        Assert.True(dirMatch.Success, "DefaultDirName must be declared in cbos-acceptance-setup.iss.");
        var acceptanceDir = dirMatch.Groups[1].Value.Trim();

        Assert.NotEqual(productionDefaultDir, acceptanceDir);
        Assert.Contains("Acceptance", acceptanceDir, StringComparison.OrdinalIgnoreCase);

        var outputBaseMatch = Regex.Match(_acceptanceIssContent, @"OutputBaseFilename=([^\r\n]+)");
        Assert.True(outputBaseMatch.Success, "OutputBaseFilename must be declared.");
        Assert.Contains("Acceptance", outputBaseMatch.Groups[1].Value, StringComparison.OrdinalIgnoreCase);

        var appNameMatch = Regex.Match(_acceptanceIssContent, @"#define\s+MyAppName\s+""([^""]+)""");
        Assert.True(appNameMatch.Success, "MyAppName must be defined.");
        Assert.Contains("Acceptance", appNameMatch.Groups[1].Value, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AcceptanceSetupScript_IncludesDatabaseProvisioner_AndSelfContainedPayload()
    {
        Assert.Contains("Clovent.Installer.Provisioner.exe", _effectiveDirectives, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ProvisionerDir", _effectiveDirectives, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ProvisionDatabase", _acceptanceIssContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Launch-Acceptance.cmd", _effectiveDirectives, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AcceptanceSetupScript_EnforcesSyntheticQaDatabase_AndRejectsOperationalTargets()
    {
        // Default target must be synthetic QA database
        Assert.Contains("CBOS_QA_Acceptance", _acceptanceIssContent, StringComparison.OrdinalIgnoreCase);

        // Code must reject operational database Clovent_BusinessOperatingSystem
        Assert.Contains("CLOVENT_BUSINESSOPERATINGSYSTEM", _acceptanceIssContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OPERATIONAL TARGET REJECTED", _acceptanceIssContent, StringComparison.OrdinalIgnoreCase);

        // Code must require approved QA prefix
        Assert.Contains("CBOS_QA_", _acceptanceIssContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CBOS_MANUALQA_", _acceptanceIssContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AcceptanceSetupScript_BlocksDestinationCollision_BothInteractivelyAndSilently()
    {
        // Interactive check on wpSelectDir
        Assert.Contains("wpSelectDir", _acceptanceIssContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("{autopf}\\Clovent\\Business Operating System", _acceptanceIssContent, StringComparison.OrdinalIgnoreCase);

        // Silent install collision guard in ssInstall
        Assert.Contains("ssInstall", _acceptanceIssContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("COLLISION GUARD", _acceptanceIssContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AcceptanceSetupScript_ShortcutsTargetLaunchAcceptance_WithQaRoot()
    {
        var iconLines = Regex.Matches(_acceptanceIssContent, @"^Name:\s*""[^""]+"";\s*Filename:\s*""([^""]+)""([^\r\n]*)", RegexOptions.Multiline);
        Assert.NotEmpty(iconLines);

        foreach (Match match in iconLines)
        {
            var targetFilename = match.Groups[1].Value.Trim();
            var remainder = match.Groups[2].Value;

            if (targetFilename.EndsWith("uninstallexe}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Assert.True(targetFilename.EndsWith("Launch-Acceptance.cmd", StringComparison.OrdinalIgnoreCase),
                $"Shortcut '{match.Value}' must target Launch-Acceptance.cmd.");
            Assert.Contains("GetEffectiveQaRoot", remainder, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void AcceptanceSetupScript_PostInstallLaunchIsOptionalAndUncheckedByDefault()
    {
        var runMatch = Regex.Match(_acceptanceIssContent, @"\[Run\]\s*\r?\n([^\r\n]+)", RegexOptions.Multiline);
        Assert.True(runMatch.Success, "Setup must contain a [Run] section.");
        var runLine = runMatch.Groups[1].Value;

        Assert.Contains("unchecked", runLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("postinstall", runLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("skipifsilent", runLine, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AcceptanceSetupScript_DoesNotSilentlyCloseOperationalProcesses()
    {
        Assert.Contains("CloseApplications=no", _effectiveDirectives, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RestartApplications=no", _effectiveDirectives, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AcceptanceSetupScript_PreservesDataOnUninstall()
    {
        Assert.Contains("CurUninstallStepChanged", _acceptanceIssContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Preserved", _acceptanceIssContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CBOS_QA_Acceptance", _acceptanceIssContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LaunchAcceptanceScript_RequiresValidatedQaRootAndApprovedTarget()
    {
        var launchScript = File.ReadAllText(_acceptanceLaunchCmdPath);

        Assert.Contains("Clovent.Desktop.exe", launchScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--qa-root", launchScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("qa-target.approved.json", launchScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CBOS_QA_Acceptance", launchScript, StringComparison.OrdinalIgnoreCase);

        // Clears ambient variables
        Assert.Contains("CBOS_QA_CONNECTION_STRING=", launchScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ConnectionStrings__Default=", launchScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ConnectionStrings__Restaurant=", launchScript, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProvisionerSource_EnforcesQaRootAndRejectsOperationalTargets()
    {
        var provProgramPath = Path.Combine(_repoRoot, "Tools", "Clovent.Installer.Provisioner", "Program.cs");
        Assert.True(File.Exists(provProgramPath), $"Expected Provisioner Program.cs at '{provProgramPath}'.");

        var provContent = File.ReadAllText(provProgramPath);

        // Supports --qa-root and --qa-mode
        Assert.Contains("--qa-root", provContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--qa-mode", provContent, StringComparison.OrdinalIgnoreCase);

        // Initializes CbosInstanceScope
        Assert.Contains("CbosInstanceScope.InitializeFromStartup", provContent, StringComparison.OrdinalIgnoreCase);

        // Rejects operational database targets
        Assert.Contains("rejected in QA acceptance mode", provContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DefaultDatabaseName", provContent, StringComparison.OrdinalIgnoreCase);

        // Writes approved target file in QA root
        Assert.Contains("QaApprovedTargetFileName", provContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("approvedData", provContent, StringComparison.OrdinalIgnoreCase);
    }
}
