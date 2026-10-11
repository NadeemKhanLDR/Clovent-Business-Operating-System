using System.Text.Json.Serialization;

namespace Clovent.Desktop.Licensing;

/// <summary>
/// Status of the software registration and license validation.
/// </summary>
public enum LicenseStatus
{
    /// <summary>The license is cryptographically valid and active.</summary>
    Valid,

    /// <summary>The license is within an authorized post-expiry grace period.</summary>
    GracePeriod,

    /// <summary>The license or evaluation period has expired.</summary>
    Expired,

    /// <summary>System clock rollback or tampering was detected.</summary>
    ClockTampered,

    /// <summary>The license hardware fingerprint does not match the current workstation.</summary>
    MachineMismatch,

    /// <summary>The cryptographic signature on the license payload is invalid or uses a revoked key.</summary>
    InvalidSignature,

    /// <summary>No license or active evaluation state is present on the workstation.</summary>
    Unlicensed
}

/// <summary>
/// Represents the payload and digital signature of a Clovent Business Operating System license.
/// Follows Requirements 4 and 19 for key versioning and comprehensive licensing metadata.
/// </summary>
public sealed class CloventLicense
{
    /// <summary>Gets or sets the cryptographic signing key identifier.</summary>
    public string KeyId { get; set; } = "clovent-2026-v2";

    /// <summary>Gets or sets the licensed product name.</summary>
    public string Product { get; set; } = "Clovent Business Operating System";

    /// <summary>Gets or sets the unique identifier of this license certificate.</summary>
    public Guid LicenseId { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the licensed customer contact name.</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the licensed company or legal entity name.</summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>Gets or sets the license tier or commercial model (e.g., Subscription, Perpetual, Trial).</summary>
    public string LicenseType { get; set; } = "Subscription";

    /// <summary>Gets or sets the UTC timestamp when the license was issued.</summary>
    public DateTimeOffset IssueDate { get; set; }

    /// <summary>Gets or sets the UTC timestamp from which the license becomes valid.</summary>
    public DateTimeOffset ValidFrom { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the license expires.</summary>
    public DateTimeOffset ExpiryDate { get; set; }

    /// <summary>Gets or sets the list of module keys enabled by this license.</summary>
    public List<string> AllowedModules { get; set; } = new();

    /// <summary>Gets or sets the maximum number of branches permitted (0 indicates unlimited).</summary>
    public int MaxBranches { get; set; } = 1; // 0 = unlimited

    /// <summary>Gets or sets the maximum number of POS terminals permitted (0 indicates unlimited).</summary>
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

    /// <summary>Gets or sets the optional workstation hardware fingerprint to which this license is locked.</summary>
    public string? MachineId { get; set; }

    /// <summary>Gets or sets the optional maintenance and support expiry timestamp.</summary>
    public DateTimeOffset? MaintenanceExpiry { get; set; }

    /// <summary>Gets or sets the Base64-encoded RSA-2048 SHA-256 signature of the canonical license payload.</summary>
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
    /// <summary>Gets the resulting validation status code.</summary>
    public LicenseStatus Status { get; init; }

    /// <summary>Gets a human-readable explanation of the license validation outcome.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Gets the validated or inspected license object, if available.</summary>
    public CloventLicense? License { get; init; }

    /// <summary>Gets the number of whole days remaining before license or evaluation expiration.</summary>
    public int DaysRemaining { get; init; }

    /// <summary>Gets a value indicating whether the workstation is operating under evaluation/trial mode.</summary>
    public bool IsEvaluation { get; init; }

    /// <summary>Gets a value indicating whether the license status authorizes full operational access.</summary>
    public bool IsAuthorized => Status is LicenseStatus.Valid or LicenseStatus.GracePeriod;
}
