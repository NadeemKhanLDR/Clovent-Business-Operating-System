using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Clovent.Desktop.Commissioning.Services;

namespace Clovent.Desktop.Licensing;

/// <summary>
/// Status of 30-day evaluation / trial mode.
/// </summary>
public enum TrialStateStatus
{
    NotStarted,
    Active,
    Expired,
    ClockRollback,
    MachineMismatch,
    CommercialSuperseded,
    Corrupted
}

/// <summary>
/// Result of evaluating workstation trial mode.
/// </summary>
public sealed record TrialEvaluationResult(
    TrialStateStatus Status,
    bool IsActive,
    int DaysRemaining,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset ExpiryDate,
    string StatusMessage);

/// <summary>
/// Model representing durable persisted evaluation state.
/// </summary>
public sealed class TrialState
{
    public string MachineId { get; set; } = string.Empty;
    public DateTimeOffset TrialStartedUtc { get; set; }
    public int DurationDays { get; set; } = 30;
    public DateTimeOffset LastVerifiedUtc { get; set; }
    public bool HasCommercialLicenseEverBeenInstalled { get; set; }
    public string HmacSignature { get; set; } = string.Empty;
}

/// <summary>
/// Manages the lifecycle, cryptographic tamper-proofing, and persistence of the 30-day evaluation mode.
/// Guarantees that:
/// 1. Trial mode persists across application and Windows restarts.
/// 2. Trial start date cannot be reset by deleting the file or reinstalling.
/// 3. Clock rollback is detected.
/// 4. Commercial license installation permanently supersedes trial.
/// 5. Customer data is never destroyed or locked upon trial expiration.
/// </summary>
public static class TrialStateManager
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };
    private static readonly byte[] HmacKey = "Clovent-CBOS-Trial-Integrity-Key-2026-v2"u8.ToArray();
    private static readonly byte[] Entropy = "Clovent-Trial-Entropy-2026"u8.ToArray();
    private static readonly object SyncLock = new();

    public const int DefaultTrialDurationDays = 30;

    private static Func<DateTimeOffset>? _timeProvider;
    private static string? _customDirectory;
    private static string? _customMachineId;
    private static Func<CommissioningMarker?>? _markerProvider;

    public static DateTimeOffset UtcNow => _timeProvider != null ? _timeProvider() : DateTimeOffset.UtcNow;
    public static string CurrentMachineId => _customMachineId ?? MachineFingerprint.GetCurrentMachineId();

    internal static void SetTestingOverrides(
        string? storageDirectory = null,
        Func<DateTimeOffset>? timeProvider = null,
        string? machineId = null,
        Func<CommissioningMarker?>? markerProvider = null)
    {
        lock (SyncLock)
        {
            _customDirectory = storageDirectory;
            _timeProvider = timeProvider;
            _customMachineId = machineId;
            _markerProvider = markerProvider;
        }
    }

    internal static void ResetTestingOverrides()
    {
        lock (SyncLock)
        {
            _customDirectory = null;
            _timeProvider = null;
            _customMachineId = null;
            _markerProvider = null;
        }
    }

    internal static string ComputeHmacForTest(TrialState state) => ComputeHmac(state);
    internal static void SaveTrialStateRawForTest(TrialState state) => SaveTrialState(state);

    /// <summary>Gets primary trial state file path in %ProgramData%.</summary>
    public static string GetPrimaryTrialStatePath()
    {
        if (!string.IsNullOrEmpty(_customDirectory))
        {
            return Path.Combine(_customDirectory, "trial.state");
        }
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var dir = Path.Combine(programData, "Clovent", "BusinessOperatingSystem", "License");
        try
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException || ex is System.Security.SecurityException)
        {
        }
        return Path.Combine(dir, "trial.state");
    }

    /// <summary>Gets fallback trial state file path in %LocalAppData%.</summary>
    public static string GetFallbackTrialStatePath()
    {
        if (!string.IsNullOrEmpty(_customDirectory))
        {
            return Path.Combine(_customDirectory, "trial.fallback.state");
        }
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(localAppData, "Clovent", "Clovent.BusinessOperatingSystem");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return Path.Combine(dir, "trial.state");
    }

    /// <summary>
    /// Starts or recovers the 30-day evaluation period for this machine.
    /// If an evaluation period has already been initiated on this machine,
    /// the original start date is strictly preserved to prevent reset.
    /// </summary>
    public static TrialEvaluationResult StartTrial(string? machineId = null, DateTimeOffset? startedAtUtc = null)
    {
        lock (SyncLock)
        {
            var currentMachineId = string.IsNullOrWhiteSpace(machineId)
                ? CurrentMachineId
                : machineId.Trim();

            var existing = LoadTrialState();
            if (existing != null)
            {
                // If trial already exists on this machine, preserve original start date
                return EvaluateTrial();
            }

            var startTime = startedAtUtc ?? UtcNow;
            var state = new TrialState
            {
                MachineId = currentMachineId,
                TrialStartedUtc = startTime,
                DurationDays = DefaultTrialDurationDays,
                LastVerifiedUtc = UtcNow,
                HasCommercialLicenseEverBeenInstalled = false
            };
            state.HmacSignature = ComputeHmac(state);

            SaveTrialState(state);
            return EvaluateTrial();
        }
    }

    /// <summary>
    /// Evaluates the current trial status of this workstation.
    /// </summary>
    public static TrialEvaluationResult EvaluateTrial()
    {
        lock (SyncLock)
        {
            var currentMachineId = CurrentMachineId;
            var state = LoadTrialState();

            if (state == null)
            {
                // Check if commissioning marker exists with IsEvaluation = true
                var marker = _markerProvider != null ? _markerProvider() : CommissioningStateService.LoadMarker();
                if (marker != null && marker.IsEvaluation)
                {
                    // Restore trial state using immutable commissioning timestamp
                    var restored = new TrialState
                    {
                        MachineId = marker.MachineId,
                        TrialStartedUtc = marker.CommissionedAtUtc,
                        DurationDays = DefaultTrialDurationDays,
                        LastVerifiedUtc = UtcNow,
                        HasCommercialLicenseEverBeenInstalled = false
                    };
                    restored.HmacSignature = ComputeHmac(restored);
                    SaveTrialState(restored);
                    state = restored;
                }
                else
                {
                    return new TrialEvaluationResult(
                        TrialStateStatus.NotStarted,
                        IsActive: false,
                        DaysRemaining: 0,
                        StartedAtUtc: default,
                        ExpiryDate: default,
                        StatusMessage: "No evaluation period has been started on this workstation.");
                }
            }

            // 1. Verify HMAC integrity
            var expectedHmac = ComputeHmac(state);
            if (!string.Equals(state.HmacSignature, expectedHmac, StringComparison.Ordinal))
            {
                return new TrialEvaluationResult(
                    TrialStateStatus.Corrupted,
                    IsActive: false,
                    DaysRemaining: 0,
                    StartedAtUtc: state.TrialStartedUtc,
                    ExpiryDate: state.TrialStartedUtc.AddDays(state.DurationDays),
                    StatusMessage: "Trial state tampering or corruption detected.");
            }

            // 2. Machine hardware binding
            if (!string.Equals(state.MachineId, currentMachineId, StringComparison.OrdinalIgnoreCase))
            {
                return new TrialEvaluationResult(
                    TrialStateStatus.MachineMismatch,
                    IsActive: false,
                    DaysRemaining: 0,
                    StartedAtUtc: state.TrialStartedUtc,
                    ExpiryDate: state.TrialStartedUtc.AddDays(state.DurationDays),
                    StatusMessage: $"Trial is bound to machine '{state.MachineId}', current machine is '{currentMachineId}'.");
            }

            // 3. Commercial license superseding check
            if (state.HasCommercialLicenseEverBeenInstalled)
            {
                return new TrialEvaluationResult(
                    TrialStateStatus.CommercialSuperseded,
                    IsActive: false,
                    DaysRemaining: 0,
                    StartedAtUtc: state.TrialStartedUtc,
                    ExpiryDate: state.TrialStartedUtc.AddDays(state.DurationDays),
                    StatusMessage: "Workstation previously activated a commercial license. Evaluation mode is superseded.");
            }

            var now = UtcNow;

            // 4. Clock rollback detection
            if (now < state.TrialStartedUtc || now < state.LastVerifiedUtc.AddHours(-1))
            {
                return new TrialEvaluationResult(
                    TrialStateStatus.ClockRollback,
                    IsActive: false,
                    DaysRemaining: 0,
                    StartedAtUtc: state.TrialStartedUtc,
                    ExpiryDate: state.TrialStartedUtc.AddDays(state.DurationDays),
                    StatusMessage: "System clock rollback detected during evaluation.");
            }

            // Update monotonic progress
            if (now > state.LastVerifiedUtc)
            {
                state.LastVerifiedUtc = now;
                state.HmacSignature = ComputeHmac(state);
                SaveTrialState(state);
            }

            var expiry = state.TrialStartedUtc.AddDays(state.DurationDays);
            var remaining = expiry - now;
            var daysRemaining = (int)Math.Ceiling(remaining.TotalDays);

            if (daysRemaining > 0 && now <= expiry)
            {
                return new TrialEvaluationResult(
                    TrialStateStatus.Active,
                    IsActive: true,
                    DaysRemaining: Math.Min(state.DurationDays, Math.Max(1, daysRemaining)),
                    StartedAtUtc: state.TrialStartedUtc,
                    ExpiryDate: expiry,
                    StatusMessage: $"Evaluation Mode ({daysRemaining} day(s) remaining)");
            }

            return new TrialEvaluationResult(
                TrialStateStatus.Expired,
                IsActive: false,
                DaysRemaining: 0,
                StartedAtUtc: state.TrialStartedUtc,
                ExpiryDate: expiry,
                StatusMessage: $"30-day evaluation period expired on {expiry:yyyy-MM-dd}. Please import a valid software license.");
        }
    }

    /// <summary>
    /// Records that a commercial license has been installed, permanently locking out trial reactivation.
    /// </summary>
    public static void RecordCommercialLicenseInstalled()
    {
        lock (SyncLock)
        {
            var state = LoadTrialState();
            if (state != null && state.HasCommercialLicenseEverBeenInstalled)
            {
                return;
            }

            state ??= new TrialState
            {
                MachineId = CurrentMachineId,
                TrialStartedUtc = UtcNow,
                DurationDays = DefaultTrialDurationDays,
                LastVerifiedUtc = UtcNow
            };

            state.HasCommercialLicenseEverBeenInstalled = true;
            state.HmacSignature = ComputeHmac(state);
            SaveTrialState(state);
        }
    }

    /// <summary>Loads trial state from disk.</summary>
    public static TrialState? LoadTrialState()
    {
        var paths = new[] { GetPrimaryTrialStatePath(), GetFallbackTrialStatePath() };
        foreach (var path in paths)
        {
            if (!File.Exists(path)) continue;

            try
            {
                var bytes = File.ReadAllBytes(path);
                byte[] decrypted;
                try
                {
                    decrypted = ProtectedData.Unprotect(bytes, Entropy, DataProtectionScope.LocalMachine);
                }
                catch
                {
                    decrypted = ProtectedData.Unprotect(bytes, Entropy, DataProtectionScope.CurrentUser);
                }

                var json = Encoding.UTF8.GetString(decrypted);
                return JsonSerializer.Deserialize<TrialState>(json, JsonOpts);
            }
            catch
            {
                // Try fallback path if any error
            }
        }

        return null;
    }

    private static void SaveTrialState(TrialState state)
    {
        var json = JsonSerializer.Serialize(state, JsonOpts);
        var bytes = Encoding.UTF8.GetBytes(json);

        byte[] protectedBytes;
        try
        {
            protectedBytes = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.LocalMachine);
        }
        catch
        {
            protectedBytes = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
        }

        try
        {
            var path = GetPrimaryTrialStatePath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllBytes(path, protectedBytes);
        }
        catch
        {
            var fallbackPath = GetFallbackTrialStatePath();
            var fallbackDir = Path.GetDirectoryName(fallbackPath);
            if (!string.IsNullOrEmpty(fallbackDir) && !Directory.Exists(fallbackDir))
            {
                Directory.CreateDirectory(fallbackDir);
            }
            File.WriteAllBytes(fallbackPath, protectedBytes);
        }
    }

    private static string ComputeHmac(TrialState state)
    {
        var payload = $"{state.MachineId}|{state.TrialStartedUtc:O}|{state.DurationDays}|{state.HasCommercialLicenseEverBeenInstalled}";
        using var hmac = new HMACSHA256(HmacKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }
}
