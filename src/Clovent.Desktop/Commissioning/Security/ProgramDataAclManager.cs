using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Clovent.Desktop.Commissioning.Security;

/// <summary>
/// Manages the %ProgramData%\Clovent\BusinessOperatingSystem\ directory tree and programmatically
/// enforces Windows DirectorySecurity Access Control Lists (ACLs).
/// </summary>
public static class ProgramDataAclManager
{
    private static readonly SecurityIdentifier AdminSid = new(WellKnownSidType.BuiltinAdministratorsSid, null);
    private static readonly SecurityIdentifier SystemSid = new(WellKnownSidType.LocalSystemSid, null);
    private static readonly SecurityIdentifier UsersSid = new(WellKnownSidType.BuiltinUsersSid, null);

    /// <summary>
    /// Root directory in %ProgramData% (or QA instance root in QA mode) for Clovent Business Operating System data.
    /// </summary>
    public static string BaseDirectory =>
        Clovent.Platform.Configuration.CbosInstanceScope.Current.MachineDataDirectory;

    /// <summary>
    /// Subdirectory for system and database configuration files.
    /// </summary>
    public static string ConfigDirectory => Clovent.Platform.Configuration.CbosInstanceScope.Current.ConfigDirectory;

    /// <summary>
    /// Subdirectory for machine-bound software licenses.
    /// </summary>
    public static string LicenseDirectory => Clovent.Platform.Configuration.CbosInstanceScope.Current.LicenseDirectory;

    /// <summary>
    /// Subdirectory for application, audit, and diagnostic logs.
    /// </summary>
    public static string LogsDirectory => Clovent.Platform.Configuration.CbosInstanceScope.Current.ProgramDataLogsDirectory;

    /// <summary>
    /// Subdirectory for machine-specific runtime state and heartbeat files.
    /// </summary>
    public static string StateDirectory => Clovent.Platform.Configuration.CbosInstanceScope.Current.StateDirectory;

    /// <summary>
    /// Subdirectory for encrypted continuity transactions journal.
    /// </summary>
    public static string ContinuityJournalDirectory => Clovent.Platform.Configuration.CbosInstanceScope.Current.ContinuityJournalDirectory;

    /// <summary>
    /// Subdirectory for operational catalog and pricing cache.
    /// </summary>
    public static string OperationalCacheDirectory => Clovent.Platform.Configuration.CbosInstanceScope.Current.OperationalCacheDirectory;

    /// <summary>
    /// Subdirectory for active cart recovery checkpoints.
    /// </summary>
    public static string CartCheckpointsDirectory => Clovent.Platform.Configuration.CbosInstanceScope.Current.CartCheckpointsDirectory;

    /// <summary>
    /// Subdirectory for durable print job tracking and quarantined print jobs.
    /// </summary>
    public static string PrintJobsDirectory => Clovent.Platform.Configuration.CbosInstanceScope.Current.PrintJobsDirectory;

    /// <summary>
    /// All managed subdirectories under <see cref="BaseDirectory"/>.
    /// </summary>
    public static string[] ManagedSubdirectories =>
    [
        ConfigDirectory,
        LicenseDirectory,
        LogsDirectory,
        StateDirectory,
        ContinuityJournalDirectory,
        OperationalCacheDirectory,
        CartCheckpointsDirectory,
        PrintJobsDirectory
    ];

    /// <summary>
    /// Creates the standard directory structure and applies hardened Windows ACLs.
    /// Administrators &amp; SYSTEM: FullControl.
    /// Built-in Users: Read-only on Config, License, State; Modify on Logs, ContinuityJournal, OperationalCache, CartCheckpoints, PrintJobs.
    /// Handles errors gracefully if running in a non-elevated or restricted context.
    /// </summary>
    /// <returns>True if all directories and ACLs were configured successfully; otherwise, false.</returns>
    public static bool ConfigureDirectorySecurity()
    {
        return ConfigureDirectorySecurity(out _);
    }

    /// <summary>
    /// Creates the standard directory structure and applies hardened Windows ACLs,
    /// returning any error message if execution fails.
    /// </summary>
    /// <param name="errorMessage">Error description if configuration fails.</param>
    /// <returns>True if all directories and ACLs were configured successfully; otherwise, false.</returns>
    public static bool ConfigureDirectorySecurity(out string? errorMessage)
    {
        errorMessage = null;

        if (!OperatingSystem.IsWindows())
        {
            errorMessage = "Directory ACLs can only be configured on Windows.";
            return false;
        }

        var scope = Clovent.Platform.Configuration.CbosInstanceScope.Current;
        if (scope.IsQaMode)
        {
            // In isolated QA mode, create the instance-owned directory tree without modifying shared %ProgramData% ACLs
            // or locking out standard-user QA launches.
            try
            {
                EnsureDirectoryExists(BaseDirectory);
                foreach (var sub in ManagedSubdirectories)
                {
                    EnsureDirectoryExists(sub);
                }
                EnsureDirectoryExists(scope.UserRootDirectory);
                EnsureDirectoryExists(scope.UserAppDirectory);
                EnsureDirectoryExists(scope.PaymentAttemptJournalDirectory);
                EnsureDirectoryExists(scope.BackupsDirectory);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to initialize QA instance directories: {ex.Message}";
                return false;
            }
        }

        try
        {
            // 1. Ensure all directories exist BEFORE hardening permissions
            EnsureDirectoryExists(BaseDirectory);
            foreach (var sub in ManagedSubdirectories)
            {
                EnsureDirectoryExists(sub);
            }

            // 2. Base directory: allow Builtin Users Read & Execute, Synchronize, and CreateDirectories
            ApplyBaseDirectorySecurity(BaseDirectory);

            // 3. Ensure and secure Config, License, State (Read-only for standard users)
            ApplyDirectorySecurity(ConfigDirectory, isLogs: false);
            ApplyDirectorySecurity(LicenseDirectory, isLogs: false);
            ApplyDirectorySecurity(StateDirectory, isLogs: false);

            // 4. Ensure and secure runtime operational stores (Read + Modify for standard users)
            ApplyDirectorySecurity(LogsDirectory, isLogs: true);
            ApplyDirectorySecurity(ContinuityJournalDirectory, isLogs: true);
            ApplyDirectorySecurity(OperationalCacheDirectory, isLogs: true);
            ApplyDirectorySecurity(CartCheckpointsDirectory, isLogs: true);
            ApplyDirectorySecurity(PrintJobsDirectory, isLogs: true);

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to configure directory ACLs: {ex.Message}";
            return false;
        }
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            try
            {
                Directory.CreateDirectory(path);
            }
            catch
            {
                // Best-effort in non-elevated or restricted execution contexts
            }
        }
    }

    private static void ApplyBaseDirectorySecurity(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var dirInfo = new DirectoryInfo(path);
        if (!dirInfo.Exists)
        {
            try
            {
                dirInfo.Create();
                dirInfo.Refresh();
            }
            catch
            {
                return;
            }
        }

        try
        {
            var security = dirInfo.GetAccessControl(AccessControlSections.Access);
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            security.PurgeAccessRules(AdminSid);
            security.PurgeAccessRules(SystemSid);
            security.PurgeAccessRules(UsersSid);

            // Administrators: FullControl
            security.AddAccessRule(new FileSystemAccessRule(
                AdminSid,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow));

            // SYSTEM: FullControl
            security.AddAccessRule(new FileSystemAccessRule(
                SystemSid,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow));

            // Built-in Users: ReadAndExecute, CreateDirectories, Synchronize
            security.AddAccessRule(new FileSystemAccessRule(
                UsersSid,
                FileSystemRights.ReadAndExecute | FileSystemRights.CreateDirectories | FileSystemRights.Synchronize,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow));

            dirInfo.SetAccessControl(security);
        }
        catch
        {
            // Best-effort in non-elevated or restricted execution contexts
        }
    }

    /// <summary>
    /// Applies directory ACLs to the specified folder path.
    /// </summary>
    /// <param name="path">Absolute directory path.</param>
    /// <param name="isLogs">If true, grants Modify permissions to Builtin Users; otherwise, read-only.</param>
    public static void ApplyDirectorySecurity(string path, bool isLogs)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var dirInfo = new DirectoryInfo(path);
        if (!dirInfo.Exists)
        {
            dirInfo.Create();
            dirInfo.Refresh();
        }

        var security = dirInfo.GetAccessControl(AccessControlSections.Access);

        // Disallow inheritance from parent directories so standard users cannot inherit write permissions
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

        // Purge any existing explicit rules for managed SIDs to ensure idempotency
        security.PurgeAccessRules(AdminSid);
        security.PurgeAccessRules(SystemSid);
        security.PurgeAccessRules(UsersSid);

        // Administrators: FullControl (ContainerInherit | ObjectInherit)
        security.AddAccessRule(new FileSystemAccessRule(
            AdminSid,
            FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Allow));

        // SYSTEM: FullControl (ContainerInherit | ObjectInherit)
        security.AddAccessRule(new FileSystemAccessRule(
            SystemSid,
            FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Allow));

        // Built-in Users:
        // Config, License, State: Read-only (ReadAndExecute, Synchronize; NO Write, NO Modify, NO Delete)
        // Logs: ReadAndExecute, Modify, Synchronize
        var userRights = isLogs
            ? FileSystemRights.ReadAndExecute | FileSystemRights.Modify | FileSystemRights.Synchronize
            : FileSystemRights.ReadAndExecute | FileSystemRights.Synchronize;

        security.AddAccessRule(new FileSystemAccessRule(
            UsersSid,
            userRights,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Allow));

        dirInfo.SetAccessControl(security);
    }
}
