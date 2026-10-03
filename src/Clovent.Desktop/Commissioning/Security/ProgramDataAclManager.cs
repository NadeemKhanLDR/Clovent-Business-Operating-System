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
    /// Root directory in %ProgramData% for Clovent Business Operating System data.
    /// </summary>
    public static string BaseDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Clovent", "BusinessOperatingSystem");

    /// <summary>
    /// Subdirectory for system and database configuration files.
    /// </summary>
    public static string ConfigDirectory => Path.Combine(BaseDirectory, "Config");

    /// <summary>
    /// Subdirectory for machine-bound software licenses.
    /// </summary>
    public static string LicenseDirectory => Path.Combine(BaseDirectory, "License");

    /// <summary>
    /// Subdirectory for application, audit, and diagnostic logs.
    /// </summary>
    public static string LogsDirectory => Path.Combine(BaseDirectory, "Logs");

    /// <summary>
    /// Subdirectory for machine-specific runtime state and heartbeat files.
    /// </summary>
    public static string StateDirectory => Path.Combine(BaseDirectory, "State");

    /// <summary>
    /// All managed subdirectories under %ProgramData%\Clovent\BusinessOperatingSystem\.
    /// </summary>
    public static readonly string[] ManagedSubdirectories =
    [
        ConfigDirectory,
        LicenseDirectory,
        LogsDirectory,
        StateDirectory
    ];

    /// <summary>
    /// Creates the standard directory structure and applies hardened Windows ACLs.
    /// Administrators &amp; SYSTEM: FullControl.
    /// Built-in Users: Read-only on Config, License, State; Modify on Logs.
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

        try
        {
            // 1. Ensure root directory exists and is secured
            ApplyDirectorySecurity(BaseDirectory, isLogs: false);

            // 2. Ensure and secure Config, License, State (Read-only for standard users)
            ApplyDirectorySecurity(ConfigDirectory, isLogs: false);
            ApplyDirectorySecurity(LicenseDirectory, isLogs: false);
            ApplyDirectorySecurity(StateDirectory, isLogs: false);

            // 3. Ensure and secure Logs (Read + Modify for standard users)
            ApplyDirectorySecurity(LogsDirectory, isLogs: true);

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to configure directory ACLs: {ex.Message}";
            return false;
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
