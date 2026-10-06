using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Clovent.Desktop.Licensing;

/// <summary>
/// Central service for loading, cryptographically validating, and managing Clovent software licenses.
/// Implements Requirements 4, 7, and 19 for secure storage, revocation verification, and tamper detection.
/// </summary>
public static class LicenseService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    private static LicenseValidationResult? _cachedResult;

    public const string ExpectedProductName = "Clovent Business Operating System";

    public static LicenseValidationResult CurrentResult => _cachedResult ??= ValidateCurrentLicense();

    public static LicenseValidationResult Refresh()
    {
        _cachedResult = ValidateCurrentLicense();
        return _cachedResult;
    }

    /// <summary>
    /// Path to the system-wide protected license file in %ProgramData%.
    /// </summary>
    public static string GetProgramDataLicensePath()
    {
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var dir = Path.Combine(programData, "Clovent", "BusinessOperatingSystem", "License");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return Path.Combine(dir, "clovent.lic");
    }

    /// <summary>
    /// Path to the per-user protected license file in %LocalAppData%.
    /// </summary>
    public static string GetLocalAppDataLicensePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(localAppData, "Clovent", "Clovent.BusinessOperatingSystem");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return Path.Combine(dir, "clovent.lic");
    }

    /// <summary>
    /// Default storage location for imported licenses. Prefers %ProgramData% with fallback to %LocalAppData%.
    /// </summary>
    public static string GetLicenseAppDataPath() => GetProgramDataLicensePath();

    private static string? _customLicenseFilePath;
    private static bool _customLicensePathSet;

    internal static void SetLicenseFilePathForTesting(string? path)
    {
        _customLicensePathSet = true;
        _customLicenseFilePath = path;
        _cachedResult = null;
    }

    internal static void ResetTestingOverrides()
    {
        _customLicensePathSet = false;
        _customLicenseFilePath = null;
        _cachedResult = null;
    }

    /// <summary>
    /// Locates an active license file across protected storage locations.
    /// Checks %ProgramData% first, then falls back to %LocalAppData%.
    /// BaseDirectory universal license loading is completely removed per Requirement 2.
    /// </summary>
    public static string? FindLicenseFilePath()
    {
        if (_customLicensePathSet)
        {
            return _customLicenseFilePath;
        }

        try
        {
            var programDataPath = GetProgramDataLicensePath();
            if (File.Exists(programDataPath))
            {
                return programDataPath;
            }
        }
        catch
        {
            // Ignore access errors on restricted environments
        }

        try
        {
            var localAppDataPath = GetLocalAppDataLicensePath();
            if (File.Exists(localAppDataPath))
            {
                return localAppDataPath;
            }
        }
        catch
        {
            // Ignore access errors
        }

        return null;
    }

    /// <summary>
    /// Validates an in-memory license object against all cryptographic, machine binding, date, and revocation checks.
    /// </summary>
    public static LicenseValidationResult ValidateLicense(CloventLicense? license)
    {
        if (license == null)
        {
            return new LicenseValidationResult
            {
                Status = LicenseStatus.InvalidSignature,
                Message = "License file is empty or corrupted.",
                License = null,
                DaysRemaining = 0
            };
        }

        // 1. Product check
        if (!string.Equals(license.Product?.Trim(), ExpectedProductName, StringComparison.OrdinalIgnoreCase))
        {
            return new LicenseValidationResult
            {
                Status = LicenseStatus.InvalidSignature,
                Message = $"License product mismatch. Expected '{ExpectedProductName}', but found '{license.Product}'.",
                License = license,
                DaysRemaining = 0
            };
        }

        // 2. KeyId check & Revocation check
        if (string.IsNullOrWhiteSpace(license.KeyId))
        {
            return new LicenseValidationResult
            {
                Status = LicenseStatus.InvalidSignature,
                Message = "License KeyId is missing or empty.",
                License = license,
                DaysRemaining = 0
            };
        }

        if (LicenseKeys.IsKeyRevoked(license.KeyId))
        {
            return new LicenseValidationResult
            {
                Status = LicenseStatus.InvalidSignature,
                Message = $"License signing key '{license.KeyId}' has been revoked or compromised. Please request an updated license.",
                License = license,
                DaysRemaining = 0
            };
        }

        var publicKeyXml = LicenseKeys.GetPublicKey(license.KeyId);
        if (string.IsNullOrWhiteSpace(publicKeyXml))
        {
            return new LicenseValidationResult
            {
                Status = LicenseStatus.InvalidSignature,
                Message = $"Unknown or untrusted license signing key '{license.KeyId}'.",
                License = license,
                DaysRemaining = 0
            };
        }

        // 3. Cryptographic signature check
        if (string.IsNullOrWhiteSpace(license.Signature))
        {
            return new LicenseValidationResult
            {
                Status = LicenseStatus.InvalidSignature,
                Message = "License digital signature is missing.",
                License = license,
                DaysRemaining = 0
            };
        }

        var canonicalPayload = license.GetCanonicalPayload();
        var payloadBytes = Encoding.UTF8.GetBytes(canonicalPayload);

        try
        {
            using var rsa = RSA.Create();
            rsa.FromXmlString(publicKeyXml);
            var signatureBytes = Convert.FromBase64String(license.Signature);
            var isValid = rsa.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            if (!isValid)
            {
                return new LicenseValidationResult
                {
                    Status = LicenseStatus.InvalidSignature,
                    Message = "Digital signature verification failed. The license file has been modified or corrupted.",
                    License = license,
                    DaysRemaining = 0
                };
            }
        }
        catch (Exception ex)
        {
            return new LicenseValidationResult
            {
                Status = LicenseStatus.InvalidSignature,
                Message = $"Cryptographic signature verification failed: {ex.Message}",
                License = license,
                DaysRemaining = 0
            };
        }

        // 4. Machine hardware binding check
        if (!string.IsNullOrWhiteSpace(license.MachineId))
        {
            var currentMachineId = MachineFingerprint.GetCurrentMachineId();
            if (!string.Equals(license.MachineId.Trim(), currentMachineId, StringComparison.OrdinalIgnoreCase))
            {
                return new LicenseValidationResult
                {
                    Status = LicenseStatus.MachineMismatch,
                    Message = $"License is bound to machine '{license.MachineId}', but current machine is '{currentMachineId}'.",
                    License = license,
                    DaysRemaining = 0
                };
            }
        }

        // 5. System clock rollback check
        if (!LicenseTamperGuard.VerifyAndUpdateClock(out var tamperMsg))
        {
            return new LicenseValidationResult
            {
                Status = LicenseStatus.ClockTampered,
                Message = tamperMsg ?? "Clock rollback detected.",
                License = license,
                DaysRemaining = 0
            };
        }

        // 6. Dates check
        var now = DateTimeOffset.UtcNow;

        if (license.ValidFrom != default && now < license.ValidFrom)
        {
            return new LicenseValidationResult
            {
                Status = LicenseStatus.Expired,
                Message = $"License is not active yet. Valid starting {license.ValidFrom:yyyy-MM-dd}.",
                License = license,
                DaysRemaining = 0
            };
        }

        var isPerpetual = string.Equals(license.LicenseType?.Trim(), "Perpetual", StringComparison.OrdinalIgnoreCase);
        if (isPerpetual)
        {
            // Perpetual license handling: Perpetual software execution remains valid;
            // MaintenanceExpiry governs update eligibility.
            var message = "Perpetual license active.";
            if (license.MaintenanceExpiry.HasValue)
            {
                if (now > license.MaintenanceExpiry.Value)
                {
                    message = $"Perpetual license active. Maintenance expired on {license.MaintenanceExpiry.Value:yyyy-MM-dd}; software update eligibility has ended.";
                }
                else
                {
                    var maintDays = (int)Math.Ceiling((license.MaintenanceExpiry.Value - now).TotalDays);
                    message = $"Perpetual license active. Maintenance active until {license.MaintenanceExpiry.Value:yyyy-MM-dd} ({maintDays} day(s) remaining).";
                }
            }

            return new LicenseValidationResult
            {
                Status = LicenseStatus.Valid,
                Message = message,
                License = license,
                DaysRemaining = int.MaxValue
            };
        }

        // Subscription or Trial
        var remainingTime = license.ExpiryDate - now;
        var daysRemaining = (int)Math.Ceiling(remainingTime.TotalDays);

        if (now > license.ExpiryDate)
        {
            // 14-day grace period
            if (now <= license.ExpiryDate.AddDays(14))
            {
                var graceDaysLeft = (int)Math.Ceiling((license.ExpiryDate.AddDays(14) - now).TotalDays);
                return new LicenseValidationResult
                {
                    Status = LicenseStatus.GracePeriod,
                    Message = $"License expired on {license.ExpiryDate:yyyy-MM-dd}. Operating under grace period ({graceDaysLeft} day(s) remaining).",
                    License = license,
                    DaysRemaining = 0
                };
            }

            return new LicenseValidationResult
            {
                Status = LicenseStatus.Expired,
                Message = $"License expired on {license.ExpiryDate:yyyy-MM-dd}. Please renew the license to continue using the system.",
                License = license,
                DaysRemaining = 0
            };
        }

        return new LicenseValidationResult
        {
            Status = LicenseStatus.Valid,
            Message = "License is valid and registered.",
            License = license,
            DaysRemaining = Math.Max(0, daysRemaining)
        };
    }

    /// <summary>
    /// Loads and validates the current software license from protected storage locations.
    /// If no commercial license file is found, evaluates whether workstation is operating
    /// within an active 30-day evaluation period.
    /// </summary>
    public static LicenseValidationResult ValidateCurrentLicense()
    {
        var filePath = FindLicenseFilePath();
        if (filePath != null)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var license = JsonSerializer.Deserialize<CloventLicense>(json, JsonOptions);
                var commercialResult = ValidateLicense(license);

                // Commercial license installation permanently supersedes evaluation mode
                TrialStateManager.RecordCommercialLicenseInstalled();
                return commercialResult;
            }
            catch (Exception ex)
            {
                return new LicenseValidationResult
                {
                    Status = LicenseStatus.InvalidSignature,
                    Message = $"Failed to validate license: {ex.Message}",
                    License = null,
                    DaysRemaining = 0
                };
            }
        }

        // No clovent.lic file found: check if active 30-day evaluation mode is active
        var trial = TrialStateManager.EvaluateTrial();
        if (trial.Status == TrialStateStatus.Active)
        {
            var trialLicense = new CloventLicense
            {
                KeyId = "evaluation-trial",
                Product = ExpectedProductName,
                LicenseId = Guid.Empty,
                CustomerName = "Evaluation Workstation",
                CompanyName = "Evaluation Mode",
                LicenseType = "Trial",
                IssueDate = trial.StartedAtUtc,
                ValidFrom = trial.StartedAtUtc,
                ExpiryDate = trial.ExpiryDate,
                MaxTerminals = 0, // Unlimited during trial
                MaxBranches = 1,
                AllowedModules = ["POS", "BackOffice", "Inventory", "Catalog", "Reporting", "Restaurant"],
                MachineId = MachineFingerprint.GetCurrentMachineId()
            };

            return new LicenseValidationResult
            {
                Status = LicenseStatus.Valid,
                IsEvaluation = true,
                Message = $"Evaluation Mode ({trial.DaysRemaining} day(s) remaining)",
                License = trialLicense,
                DaysRemaining = trial.DaysRemaining
            };
        }

        if (trial.Status == TrialStateStatus.Expired)
        {
            var expiredTrialLicense = new CloventLicense
            {
                KeyId = "evaluation-trial",
                Product = ExpectedProductName,
                LicenseId = Guid.Empty,
                CustomerName = "Evaluation Workstation",
                CompanyName = "Evaluation Mode (Expired)",
                LicenseType = "Trial",
                IssueDate = trial.StartedAtUtc,
                ValidFrom = trial.StartedAtUtc,
                ExpiryDate = trial.ExpiryDate,
                MaxTerminals = 0,
                MaxBranches = 1,
                AllowedModules = ["POS", "BackOffice", "Inventory", "Catalog", "Reporting", "Restaurant"],
                MachineId = MachineFingerprint.GetCurrentMachineId()
            };

            return new LicenseValidationResult
            {
                Status = LicenseStatus.Expired,
                IsEvaluation = true,
                Message = $"30-day evaluation period expired on {trial.ExpiryDate:yyyy-MM-dd}. Please import a valid software license.",
                License = expiredTrialLicense,
                DaysRemaining = 0
            };
        }

        if (trial.Status == TrialStateStatus.ClockRollback)
        {
            return new LicenseValidationResult
            {
                Status = LicenseStatus.ClockTampered,
                IsEvaluation = true,
                Message = "System clock rollback detected during evaluation period.",
                License = null,
                DaysRemaining = 0
            };
        }

        if (trial.Status == TrialStateStatus.CommercialSuperseded)
        {
            return new LicenseValidationResult
            {
                Status = LicenseStatus.Unlicensed,
                IsEvaluation = false,
                Message = "Commercial license was previously active on this workstation. Please import a renewed software license.",
                License = null,
                DaysRemaining = 0
            };
        }

        return new LicenseValidationResult
        {
            Status = LicenseStatus.Unlicensed,
            IsEvaluation = false,
            Message = "No software license file (clovent.lic) was found. Please register or import a valid license.",
            License = null,
            DaysRemaining = 0
        };
    }

    /// <summary>
    /// Imports a candidate license file into active protected storage.
    /// Validates BEFORE copying; if signature or schema is invalid, active license is NOT overwritten.
    /// </summary>
    public static LicenseValidationResult ImportLicense(string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Specified license file does not exist.", sourceFilePath);
        }

        var json = File.ReadAllText(sourceFilePath);
        CloventLicense? candidateLicense;
        try
        {
            candidateLicense = JsonSerializer.Deserialize<CloventLicense>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            return new LicenseValidationResult
            {
                Status = LicenseStatus.InvalidSignature,
                Message = $"License file could not be parsed: {ex.Message}",
                License = null,
                DaysRemaining = 0
            };
        }

        // Validate BEFORE copying into active storage!
        var validationResult = ValidateLicense(candidateLicense);
        if (!validationResult.IsAuthorized)
        {
            // Do NOT overwrite the currently valid license file!
            return validationResult;
        }

        // Try %ProgramData% first, fallback to %LocalAppData%
        try
        {
            var programDataTarget = GetProgramDataLicensePath();
            var dir = Path.GetDirectoryName(programDataTarget);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.Copy(sourceFilePath, programDataTarget, overwrite: true);
        }
        catch
        {
            // Non-administrator permission fallback
            var localAppDataTarget = GetLocalAppDataLicensePath();
            var dir = Path.GetDirectoryName(localAppDataTarget);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.Copy(sourceFilePath, localAppDataTarget, overwrite: true);
        }

        return Refresh();
    }

    /// <summary>
    /// Helper to verify whether the system is authorized to process transactions.
    /// Returns true if Valid or GracePeriod; false if Expired, ClockTampered, MachineMismatch, InvalidSignature, or Unlicensed.
    /// </summary>
    public static bool CanCreateTransactions()
    {
        var status = CurrentResult.Status;
        return status is LicenseStatus.Valid or LicenseStatus.GracePeriod;
    }
}
