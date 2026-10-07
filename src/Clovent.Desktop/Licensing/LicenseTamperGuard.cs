using System.Security.Cryptography;
using System.Text;

namespace Clovent.Desktop.Licensing;

/// <summary>
/// Detects system clock rollback tampering by recording encrypted execution timestamps in AppData.
/// </summary>
public static class LicenseTamperGuard
{
    private static readonly byte[] Entropy = "Clovent-Lic-Guard-Entropy-v1"u8.ToArray();
    private static readonly object SyncLock = new();
    private static string? _customGuardFilePath;

    /// <summary>Sets testing overrides for the guard file path.</summary>
    public static void SetTestingOverrides(string? customGuardFilePath = null)
    {
        lock (SyncLock)
        {
            _customGuardFilePath = customGuardFilePath;
        }
    }

    /// <summary>Resets testing overrides.</summary>
    public static void ResetTestingOverrides()
    {
        lock (SyncLock)
        {
            _customGuardFilePath = null;
        }
    }

    /// <summary>Gets the effective path of license_guard.dat.</summary>
    public static string GetGuardFilePath()
    {
        lock (SyncLock)
        {
            if (!string.IsNullOrEmpty(_customGuardFilePath))
            {
                var customDir = Path.GetDirectoryName(_customGuardFilePath);
                if (!string.IsNullOrEmpty(customDir) && !Directory.Exists(customDir))
                {
                    Directory.CreateDirectory(customDir);
                }
                return _customGuardFilePath;
            }
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(localAppData, "Clovent", "Clovent.BusinessOperatingSystem");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return Path.Combine(dir, "license_guard.dat");
    }

    /// <summary>
    /// Checks whether the system clock has been set backward relative to previous executions.
    /// If valid, records the current UTC timestamp.
    /// </summary>
    /// <returns>True if clock is valid; False if rollback tampering is detected.</returns>
    public static bool VerifyAndUpdateClock(out string? tamperingMessage)
    {
        var path = GetGuardFilePath();
        var now = DateTimeOffset.UtcNow;
        var needsWrite = true;

        if (File.Exists(path))
        {
            try
            {
                var cipherBytes = File.ReadAllBytes(path);
                var plainBytes = ProtectedData.Unprotect(cipherBytes, Entropy, DataProtectionScope.CurrentUser);
                var text = Encoding.UTF8.GetString(plainBytes);
                if (DateTimeOffset.TryParse(text, out var lastRecordedTime))
                {
                    // Allow up to 2 hours of leeway for minor daylight savings / NTP adjustments
                    if (now < lastRecordedTime.AddHours(-2))
                    {
                        tamperingMessage = $"System clock rollback detected. Current time ({now:yyyy-MM-dd HH:mm:ss} UTC) is earlier than previous run ({lastRecordedTime:yyyy-MM-dd HH:mm:ss} UTC).";
                        return false;
                    }

                    // If clock was verified recently (within 5 minutes) and is progressing forward, skip redundant file write
                    if (now >= lastRecordedTime && (now - lastRecordedTime).TotalMinutes < 5)
                    {
                        needsWrite = false;
                    }
                }
            }
            catch
            {
                // If the file is corrupted or cannot be decrypted, proceed with recording current time
            }
        }

        // In automated test execution without explicit testing override, prevent modifying the real workstation file
        if (IsTestEnvironment() && string.IsNullOrEmpty(_customGuardFilePath))
        {
            needsWrite = false;
        }

        if (needsWrite)
        {
            // Record updated time
            try
            {
                var plainBytes = Encoding.UTF8.GetBytes(now.ToString("O"));
                var cipherBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(path, cipherBytes);
            }
            catch
            {
                // Ignore write failures in read-only environments
            }
        }

        tamperingMessage = null;
        return true;
    }

    private static bool IsTestEnvironment()
    {
        try
        {
            var friendlyName = AppDomain.CurrentDomain.FriendlyName;
            if (friendlyName.Contains("testhost", StringComparison.OrdinalIgnoreCase) ||
                friendlyName.Contains("vstest", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var cmd = Environment.CommandLine;
            if (cmd.Contains("testhost", StringComparison.OrdinalIgnoreCase) ||
                cmd.Contains("vstest", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                var name = assemblies[i].GetName().Name;
                if (name != null && (name.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) ||
                                     name.StartsWith("Microsoft.TestPlatform", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
        }
        catch
        {
        }
        return false;
    }
}
