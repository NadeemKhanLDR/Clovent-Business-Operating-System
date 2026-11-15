using System;
using System.IO;
using Xunit;

namespace Clovent.Desktop.Tests.Packaging;

public sealed class ProductionInstallerPackagingTests
{
    private static string FindRepoRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "Clovent.sln")) ||
                File.Exists(Path.Combine(current, "Clovent.BusinessOperatingSystem.slnx")) ||
                Directory.Exists(Path.Combine(current, ".git")))
            {
                return current;
            }

            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }

        return @"D:\CBOS\Worktrees\cbos-transaction-integration-review";
    }

    [Fact]
    public void ProductionSetupScript_HasStandardProductionIdentity_AndNoQaLabels()
    {
        var repoRoot = FindRepoRoot();
        var issPath = Path.Combine(repoRoot, "Tools", "installer", "cbos-setup.iss");
        Assert.True(File.Exists(issPath), $"Production installer script not found at: {issPath}");

        var content = File.ReadAllText(issPath);

        // Standard production identity
        Assert.Contains("#define MyAppName \"Clovent Business Operating System\"", content);
        Assert.Contains("#define MyAppVersion \"1.3.0\"", content);
        Assert.Contains("#define MyAppId \"{{C107E47D-CB05-47F1-9BD7-A107CB052026}}\"", content);
        Assert.Contains("DefaultDirName={autopf}\\Clovent\\Business Operating System", content);
        Assert.Contains("OutputBaseFilename={#OutputBaseFilename}", content);

        // Standard shortcuts launching Clovent.Desktop.exe directly
        Assert.Contains("Name: \"{autoprograms}\\{#MyAppName}\\{#MyAppName}\"; Filename: \"{app}\\{#MyAppExeName}\"", content);
        Assert.Contains("Name: \"{autodesktop}\\{#MyAppName}\"; Filename: \"{app}\\{#MyAppExeName}\"", content);

        // Strict absence of QA / Acceptance environment and launchers
        Assert.DoesNotContain("Launch-Acceptance.cmd", content);
        Assert.DoesNotContain("Launch-QA.cmd", content);
        Assert.DoesNotContain("CBOS_QA_ISOLATED_ROOT", content);
        Assert.DoesNotContain("--qa-root", content);
        Assert.DoesNotContain("--qa-mode", content);
        Assert.DoesNotContain("CBOS_QA_Acceptance", content);
    }

    [Fact]
    public void ProductionSetupScript_EnforcesProcessTerminationSafety_AndCollisionGuard()
    {
        var repoRoot = FindRepoRoot();
        var issPath = Path.Combine(repoRoot, "Tools", "installer", "cbos-setup.iss");
        var content = File.ReadAllText(issPath);

        // Never silently close operational CBOS processes
        Assert.Contains("CloseApplications=no", content);
        Assert.Contains("IsProcessRunning('Clovent.Desktop.exe')", content);
        Assert.Contains("Please save your work and close the application before proceeding with installation", content);

        // Cross-edition collision protection
        Assert.Contains("Pos('Acceptance', SelectedDir) > 0", content);
        Assert.Contains("Pos('CBOS_QA', SelectedDir) > 0", content);

        // Downgrade protection
        Assert.Contains("CompareText(PrevVersion, '{#MyAppVersion}') > 0", content);
        Assert.Contains("Downgrading to version {#MyAppVersion} is not supported", content);
    }

    [Fact]
    public void ProductionSetupScript_ConfiguresProductionDatabase_WithPreMigrationBackup()
    {
        var repoRoot = FindRepoRoot();
        var issPath = Path.Combine(repoRoot, "Tools", "installer", "cbos-setup.iss");
        var content = File.ReadAllText(issPath);

        // Default production database
        Assert.Contains("TxtDatabase.Text := 'Clovent_BusinessOperatingSystem'", content);

        // Pre-migration verified backup argument
        Assert.Contains("--backup-before-migration", content);
        Assert.Contains("Verified pre-migration database backup failed", content);

        // Explicit user confirmation prompt before execution
        Assert.Contains("Ready to proceed with Clovent Business Operating System installation and database provisioning", content);
        Assert.Contains("Do you want to continue?", content);
        Assert.Contains("MsgBox(ConfirmMsg, mbConfirmation, MB_YESNO) <> IDYES", content);
    }

    [Fact]
    public void ProductionSetupScript_ConfiguresOfflinePrerequisites_AndRebootHandling()
    {
        var repoRoot = FindRepoRoot();
        var issPath = Path.Combine(repoRoot, "Tools", "installer", "cbos-setup.iss");
        var content = File.ReadAllText(issPath);

        // Prerequisite file embedding
        Assert.Contains("Source: \"{#PrerequisitesDir}\\SQLEXPR_x64_ENU.exe\"", content);
        Assert.Contains("Source: \"{#PrerequisitesDir}\\SSMS-Setup-ENU.exe\"", content);
        Assert.Contains("Source: \"{#PrerequisitesDir}\\VC_redist.x64.exe\"", content);
        Assert.Contains("Source: \"{#PrerequisitesDir}\\msoledbsql.msi\"", content);

        // Prerequisite ordering and execution
        Assert.Contains("InstallVcRedist()", content);
        Assert.Contains("InstallMsoledbsql()", content);
        Assert.Contains("InstallSqlServerExpress()", content);
        Assert.Contains("ProvisionDatabase()", content);
        Assert.Contains("InstallSsms()", content);

        // Reboot handling without forced restart
        Assert.Contains("RequiresReboot := True", content);
        Assert.Contains("function NeedRestart(): Boolean", content);
        Assert.Contains("Result := RequiresReboot", content);
    }

    [Fact]
    public void ProductionSetupScript_PreservesCustomerDataAndDatabasesOnUninstall()
    {
        var repoRoot = FindRepoRoot();
        var issPath = Path.Combine(repoRoot, "Tools", "installer", "cbos-setup.iss");
        var content = File.ReadAllText(issPath);

        // Uninstall preservation
        Assert.Contains("Uninstaller removed application binaries and shortcuts", content);
        Assert.Contains("Customer database [' + SelectedDatabase + '], backups, licenses, and %ProgramData% configurations remain preserved", content);

        // Application launch unchecked by default
        Assert.Contains("Filename: \"{app}\\{#MyAppExeName}\"; Description: \"{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}\"; Flags: nowait postinstall skipifsilent unchecked", content);
    }

    [Fact]
    public void ProvisionerCli_SupportsBackupBeforeMigrationFlag()
    {
        var repoRoot = FindRepoRoot();
        var provSource = Path.Combine(repoRoot, "Tools", "Clovent.Installer.Provisioner", "Program.cs");
        var content = File.ReadAllText(provSource);

        Assert.Contains("--backup-before-migration", content);
        Assert.Contains("--backup-dir", content);
        Assert.Contains("ExitCodeBackupFailed = 5", content);
        Assert.Contains("Executing verified pre-migration backup (COPY_ONLY)", content);
    }

    [Fact]
    public void ProductionSetupScript_ConfiguresSetupIconFile_AndUninstallDisplayIcon()
    {
        var repoRoot = FindRepoRoot();
        var issPath = Path.Combine(repoRoot, "Tools", "installer", "cbos-setup.iss");
        var content = File.ReadAllText(issPath);

        // SetupIconFile and UninstallDisplayIcon directives
        Assert.Contains("SetupIconFile={#SetupIconPath}", content);
        Assert.Contains("UninstallDisplayIcon={app}\\Clovent.Desktop.exe,0", content);
        Assert.Contains("#define SetupIconPath \"..\\..\\src\\Clovent.Desktop\\Resources\\cbos.ico\"", content);

        // Icon deployment in [Files]
        Assert.Contains("Source: \"{#SetupIconPath}\"; DestDir: \"{app}\\Resources\"; DestName: \"cbos.ico\"", content);
    }

    [Fact]
    public void ApprovedBrandIcon_ContainsRequiredMultiResolutionImages()
    {
        var repoRoot = FindRepoRoot();
        var iconPath = Path.Combine(repoRoot, "src", "Clovent.Desktop", "Resources", "cbos.ico");
        Assert.True(File.Exists(iconPath), $"cbos.ico not found at {iconPath}");

        using var fs = File.OpenRead(iconPath);
        using var br = new BinaryReader(fs);

        var reserved = br.ReadUInt16();
        var type = br.ReadUInt16();
        var count = br.ReadUInt16();

        Assert.Equal(0, reserved);
        Assert.Equal(1, type); // 1 = ICO
        Assert.Equal(7, count); // 7 resolutions

        int[] expectedWidths = [16, 24, 32, 48, 64, 128, 256];
        for (int i = 0; i < count; i++)
        {
            var w = br.ReadByte();
            var h = br.ReadByte();
            var width = w == 0 ? 256 : (int)w;
            var height = h == 0 ? 256 : (int)h;

            br.ReadByte(); // colors
            br.ReadByte(); // reserved
            br.ReadUInt16(); // planes
            var bpp = br.ReadUInt16();
            br.ReadUInt32(); // bytes in res
            br.ReadUInt32(); // image offset

            Assert.Equal(expectedWidths[i], width);
            Assert.Equal(expectedWidths[i], height);
            Assert.Equal(32, bpp); // 32-bit ARGB
        }
    }

    [Fact]
    public void AuthenticodeSigningPipeline_ImplementsPreAndPostSigning_AndDistinguishesUnsignedCandidates()
    {
        var repoRoot = FindRepoRoot();
        var signScriptPath = Path.Combine(repoRoot, "Tools", "sign_release_payloads.ps1");
        Assert.True(File.Exists(signScriptPath), $"sign_release_payloads.ps1 not found at {signScriptPath}");

        var content = File.ReadAllText(signScriptPath);

        // SHA-256 and RFC 3161 timestamping
        Assert.Contains("/fd", content);
        Assert.Contains("sha256", content);
        Assert.Contains("http://timestamp.digicert.com", content);
        Assert.Contains("/tr", content);
        Assert.Contains("/td", content);

        // Signature verification
        Assert.Contains("signtool", content);
        Assert.Contains("verify", content);
        Assert.Contains("/pa", content);
        Assert.Contains("Get-AuthenticodeSignature", content);

        // Preserves third-party prerequisites
        Assert.Contains("Preserving third-party Microsoft signature on prerequisite", content);

        // Clear separation of unsigned candidate vs signed production
        Assert.Contains("AllowUnsignedCandidate", content);
        Assert.Contains("Self-signed certificates are strictly prohibited as production-trust substitutes", content);
        Assert.Contains("Unsigned (RELEASE CANDIDATE - PENDING COMMERCIAL AUTHENTICODE SIGNING)", content);
    }

    [Fact]
    public void ApplicationControlDiagnosticCollector_IsReadOnly_AndCollectsCodeIntegrityXmlEvents()
    {
        var repoRoot = FindRepoRoot();
        var collectorPath = Path.Combine(repoRoot, "Tools", "diagnostics", "Collect-AppControlEvidence.ps1");
        Assert.True(File.Exists(collectorPath), $"Collect-AppControlEvidence.ps1 not found at {collectorPath}");

        var content = File.ReadAllText(collectorPath);

        // Strictly read-only
        Assert.Contains("Mode: Strictly Read-Only Diagnostic Inspection", content);

        // Queries CodeIntegrity and AppLocker event logs
        Assert.Contains("Microsoft-Windows-CodeIntegrity/Operational", content);
        Assert.Contains("Microsoft-Windows-AppLocker/EXE and DLL", content);
        Assert.Contains("ToXml()", content);

        // Checks Smart App Control & WDAC policies
        Assert.Contains("VerifiedAndReputablePolicyState", content);
        Assert.Contains("CiTool", content);

        // Distinguishes outer setup EXE from extracted payloads
        Assert.Contains("OuterSetupExecutable", content);
        Assert.Contains("InnoSetupExtractedTempBinary", content);
        Assert.Contains("FirstPartyOwnedPayload", content);
        Assert.Contains("ThirdPartyPrerequisiteMedia", content);
    }

    [Fact]
    public void PackagingPipeline_IntegratesIconVerification_AndSigningPhases()
    {
        var repoRoot = FindRepoRoot();
        var packageScript = Path.Combine(repoRoot, "Tools", "package_production_candidate.ps1");
        var content = File.ReadAllText(packageScript);

        // Icon parameter and verification
        Assert.Contains("/DSetupIconPath=", content);
        Assert.Contains("verify_installer_icon.ps1", content);

        // Signing stages
        Assert.Contains("sign_release_payloads.ps1", content);
        Assert.Contains("Authenticode Code Signing (Stage 1: Owned Payloads)", content);
        Assert.Contains("Authenticode Code Signing (Stage 2: Installer Executable)", content);

        // Copies diagnostic evidence collector
        Assert.Contains("Collect-AppControlEvidence.ps1", content);

        // Provenance metadata contains icon and signature sections
        Assert.Contains("IconVerification", content);
        Assert.Contains("AuthenticodeSignature", content);
    }
}
