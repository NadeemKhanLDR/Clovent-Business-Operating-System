using System.Text.Json.Serialization;

namespace Clovent.Desktop.Licensing;

/// <summary>
/// Status of the software registration and license validation.
/// </summary>
public enum LicenseStatus
{
    Valid,
    GracePeriod,
    Expired,
    ClockTampered,
    MachineMismatch,
    InvalidSignature,
    Unlicensed
}

/// <summary>
/// Represents the payload and digital signature of a Clovent Business Operating System license.
/// Follows Requirements 4 and 19 for key versioning and comprehensive licensing metadata.
/// </summary>
public sealed class CloventLicense
{
    public string KeyId { get; set; } = "clovent-2026-v2";

    public string Product { get; set; } = "Clovent Business Operating System";

    public Guid LicenseId { get; set; } = Guid.NewGuid();

    public string CustomerName { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string LicenseType { get; set; } = "Subscription";

    public DateTimeOffset IssueDate { get; set; }

    public DateTimeOffset ValidFrom { get; set; }

    public DateTimeOffset ExpiryDate { get; set; }

    public List<string> AllowedModules { get; set; } = new();

    public int MaxBranches { get; set; } = 1; // 0 = unlimited

    public int MaxTerminals { get; set; } = 1; // 0 = unlimited

    /// <summary>
    /// Backwards-compatibility alias for <see cref="MaxTerminals"/>.
    /// </summary>
    [JsonIgnore]
    public int TerminalLimit
    {
        get => MaxTerminals;
        set => MaxTerminals = value;
    }

    public string? MachineId { get; set; }

    public DateTimeOffset? MaintenanceExpiry { get; set; }

    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Computes the canonical UTF-8 payload representation for cryptographic signing and verification.
    /// Strictly formatted per Requirement 4 and Requirement 19.
    /// </summary>
    public string GetCanonicalPayload()
    {
        var modules = AllowedModules != null ? string.Join(",", AllowedModules.OrderBy(x => x)) : string.Empty;
        var machine = MachineId?.Trim() ?? string.Empty;
        return $"{KeyId}|{Product}|{LicenseId:D}|{(CustomerName ?? string.Empty).Trim()}|{(CompanyName ?? string.Empty).Trim()}|{(LicenseType ?? string.Empty).Trim()}|{IssueDate:O}|{ValidFrom:O}|{ExpiryDate:O}|{(MaintenanceExpiry.HasValue ? MaintenanceExpiry.Value.ToString("O") : "")}|{MaxBranches}|{MaxTerminals}|{modules}|{machine}";
    }
}

/// <summary>
/// Result of verifying a license.
/// </summary>
public sealed class LicenseValidationResult
{
    public LicenseStatus Status { get; init; }

    public string Message { get; init; } = string.Empty;

    public CloventLicense? License { get; init; }

    public int DaysRemaining { get; init; }

    public bool IsEvaluation { get; init; }

    public bool IsAuthorized => Status is LicenseStatus.Valid or LicenseStatus.GracePeriod;
}
