using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Clovent.Desktop.Tests.TestSupport;
using Xunit;

namespace Clovent.Desktop.Tests.Packaging;

/// <summary>
/// Regression and safety checks proving the internal QA installer variant (cbos-qa-setup.iss)
/// satisfies strict safety constraints: application files only, no automatic SQL download/install,
/// no database provisioning or migration, no automatic launch or normal-mode shortcuts, no
/// modification of production ProgramData ACLs, and separate upgrade identity.
/// </summary>
public sealed class QaInstallerPackagingTests
{
    private readonly string _repoRoot;
    private readonly string _qaIssPath;
    private readonly string _qaLaunchCmdPath;
    private readonly string _qaIssContent;
    private readonly string _effectiveDirectives;

    public QaInstallerPackagingTests()
    {
        _repoRoot = RepositoryPaths.FindRepositoryRoot();
        _qaIssPath = Path.Combine(_repoRoot, "Tools", "installer", "cbos-qa-setup.iss");
        _qaLaunchCmdPath = Path.Combine(_repoRoot, "Tools", "installer", "Launch-QA.cmd");

        Assert.True(File.Exists(_qaIssPath), $"Expected QA installer script at '{_qaIssPath}'.");
        Assert.True(File.Exists(_qaLaunchCmdPath), $"Expected QA launcher script at '{_qaLaunchCmdPath}'.");

        _qaIssContent = File.ReadAllText(_qaIssPath);
        _effectiveDirectives = string.Join(
            Environment.NewLine,
            _qaIssContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                .Where(line => !line.TrimStart().StartsWith(";")));
    }

    [Fact]
    public void QaSetupScript_HasDistinctAppIdAndInstallationDirectory_PreventingOperationalCollision()
    {
        const string productionAppId = "{{C107E47D-CB05-47F1-9BD7-A107CB052026}}";
        const string productionDefaultDir = @"{autopf}\Clovent\Business Operating System";

        var appIdMatch = Regex.Match(_qaIssContent, @"#define\s+MyAppId\s+""([^""]+)""");
        Assert.True(appIdMatch.Success, "MyAppId must be defined in cbos-qa-setup.iss.");
        var qaAppId = appIdMatch.Groups[1].Value.Trim();

        Assert.NotEqual(productionAppId, qaAppId);
        Assert.Contains("QA", qaAppId, StringComparison.OrdinalIgnoreCase);

        var dirMatch = Regex.Match(_qaIssContent, @"DefaultDirName=([^\r\n]+)");
        Assert.True(dirMatch.Success, "DefaultDirName must be declared in cbos-qa-setup.iss.");
        var qaDir = dirMatch.Groups[1].Value.Trim();

        Assert.NotEqual(productionDefaultDir, qaDir);
        Assert.Contains("QA", qaDir, StringComparison.OrdinalIgnoreCase);

        var outputBaseMatch = Regex.Match(_qaIssContent, @"OutputBaseFilename=([^\r\n]+)");
        Assert.True(outputBaseMatch.Success, "OutputBaseFilename must be declared.");
        Assert.Contains("QA", outputBaseMatch.Groups[1].Value, StringComparison.OrdinalIgnoreCase);

        var appNameMatch = Regex.Match(_qaIssContent, @"#define\s+MyAppName\s+""([^""]+)""");
        Assert.True(appNameMatch.Success, "MyAppName must be defined.");
        Assert.Contains("QA", appNameMatch.Groups[1].Value, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void QaSetupScript_ExcludesSqlDownloadAndInstallation()
    {
        Assert.DoesNotContain("InstallSqlServerExpress", _effectiveDirectives, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SQLEXPR_x64_ENU", _effectiveDirectives, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("download.microsoft.com", _effectiveDirectives, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DetectSqlServer", _effectiveDirectives, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sc query", _effectiveDirectives, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("net.exe", _effectiveDirectives, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MSSQLSERVER", _effectiveDirectives, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MSSQL$", _effectiveDirectives, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void QaSetupScript_ExcludesDatabaseProvisioningAndProvisionerPayload()
    {
        Assert.DoesNotContain("ProvisionDatabase", _effectiveDirectives, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("--provision", _effectiveDirectives, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Clovent.Installer.Provisioner", _effectiveDirectives, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ProvisionerDir", _effectiveDirectives, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void QaSetupScript_DoesNotTouchSharedProductionProgramDataOrAcls()
    {
        // Must not contain any [Dirs] configuring {commonappdata}\Clovent\BusinessOperatingSystem
        Assert.DoesNotContain("{commonappdata}\\Clovent\\BusinessOperatingSystem", _effectiveDirectives, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Permissions: system-full admins-full", _effectiveDirectives, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void QaSetupScript_NeverLaunchesAutomatically_AndExcludesNormalModeShortcuts()
    {
        // No [Run] section launching desktop binary
        Assert.DoesNotContain("[Run]", _effectiveDirectives, StringComparison.OrdinalIgnoreCase);

        // Icons must not point directly to Clovent.Desktop.exe without QA flags
        var iconLines = Regex.Matches(_qaIssContent, @"^Name:\s*""[^""]+"";\s*Filename:\s*""([^""]+)""([^\r\n]*)", RegexOptions.Multiline);
        Assert.NotEmpty(iconLines);

        foreach (Match match in iconLines)
        {
            var targetFilename = match.Groups[1].Value.Trim();
            var remainder = match.Groups[2].Value;

            if (targetFilename.EndsWith("uninstallexe}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Any application shortcut must launch Launch-QA.cmd or pass explicit QA parameters
            var isQaLaunchCmd = targetFilename.EndsWith("Launch-QA.cmd", StringComparison.OrdinalIgnoreCase);
            var hasQaParams = remainder.Contains("--qa-root", StringComparison.OrdinalIgnoreCase);

            Assert.True(isQaLaunchCmd || hasQaParams,
                $"Shortcut '{match.Value}' must target Launch-QA.cmd or include explicit --qa-root argument.");
        }
    }

    [Fact]
    public void QaSetupScript_BlocksDestinationCollisionWithOperationalProductionDirectory()
    {
        // Must contain NextButtonClick check preventing user override to operational folder or registered install
        Assert.Contains("NextButtonClick", _qaIssContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("wpSelectDir", _qaIssContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("{autopf}\\Clovent\\Business Operating System", _qaIssContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("{C107E47D-CB05-47F1-9BD7-A107CB052026}", _qaIssContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void QaLaunchScript_RequiresValidatedQaRootAndApprovedTarget()
    {
        var launchScript = File.ReadAllText(_qaLaunchCmdPath);

        Assert.Contains("Clovent.Desktop.exe", launchScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--qa-root", launchScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("qa-target.approved.json", launchScript, StringComparison.OrdinalIgnoreCase);

        // Ensures application contract environment variables are supported and cleared
        Assert.Contains("CBOS_QA_INSTANCE_ROOT", launchScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CBOS_QA_CONNECTION_STRING=", launchScript, StringComparison.OrdinalIgnoreCase);

        // Ensures ambient production variables are cleared
        Assert.Contains("ConnectionStrings__Default=", launchScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ConnectionStrings__Restaurant=", launchScript, StringComparison.OrdinalIgnoreCase);

        // Ensures empty approved target file is rejected
        Assert.Contains("%%~zF", launchScript, StringComparison.OrdinalIgnoreCase);
    }
}
