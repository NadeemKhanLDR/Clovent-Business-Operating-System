using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace Clovent.Desktop.Licensing;

/// <summary>
/// Provides a stable, non-invasive machine hardware fingerprint for node-locked licensing.
/// </summary>
public static class MachineFingerprint
{
    private static string? _cachedFingerprint;

    /// <summary>
    /// Gets the machine fingerprint formatted as 4-block hexadecimal string (e.g. ABCD-EF01-2345-6789).
    /// </summary>
    public static string GetCurrentMachineId()
    {
        if (_cachedFingerprint != null)
        {
            return _cachedFingerprint;
        }

        string rawEntropy;
        try
        {
            var machineGuid = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography")?.GetValue("MachineGuid")?.ToString();
            if (!string.IsNullOrWhiteSpace(machineGuid))
            {
                rawEntropy = machineGuid;
            }
            else
            {
                rawEntropy = $"{Environment.MachineName}|{Environment.ProcessorCount}|{Environment.OSVersion.VersionString}";
            }
        }
        catch
        {
            rawEntropy = $"{Environment.MachineName}|{Environment.ProcessorCount}|{Environment.UserName}";
        }

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawEntropy));
        var hex = Convert.ToHexString(hashBytes)[..16];
        _cachedFingerprint = $"{hex[..4]}-{hex[4..8]}-{hex[8..12]}-{hex[12..16]}";
        return _cachedFingerprint;
    }
}
