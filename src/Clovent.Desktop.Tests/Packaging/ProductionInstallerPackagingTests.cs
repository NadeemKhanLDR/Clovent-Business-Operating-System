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
}
