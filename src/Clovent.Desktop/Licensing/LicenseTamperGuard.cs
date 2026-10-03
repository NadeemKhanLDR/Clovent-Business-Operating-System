using System.Security.Cryptography;
using System.Text;

namespace Clovent.Desktop.Licensing;

/// <summary>
/// Detects system clock rollback tampering by recording encrypted execution timestamps in AppData.
/// </summary>
public static class LicenseTamperGuard
{
    private static readonly byte[] Entropy = "Clovent-Lic-Guard-Entropy-v1"u8.ToArray();

    private static string GetGuardFilePath()
    {
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
                }
            }
            catch
            {
                // If the file is corrupted or cannot be decrypted, proceed with recording current time
            }
        }

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

        tamperingMessage = null;
        return true;
    }
}
