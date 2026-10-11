using Clovent.Desktop.Configuration;

namespace Clovent.Desktop.Commissioning.Services;

/// <summary>
/// Input parameters collected from the UI to execute first-run system commissioning.
/// </summary>
public sealed class CommissioningExecutionRequest
{
    /// <summary>Gets the target database connection settings.</summary>
    public required DatabaseConnectionSettings ConnectionSettings { get; init; }

    /// <summary>Gets the foundational master data provisioning request.</summary>
    public required InitialMasterDataProvisioningRequest MasterDataRequest { get; init; }

    /// <summary>Gets the initial administrator account provisioning request.</summary>
    public required FirstAdminProvisioningRequest AdminRequest { get; init; }

    /// <summary>Gets the commissioning completion marker metadata.</summary>
    public required CommissioningMarker Marker { get; init; }
}

/// <summary>
/// Result of the end-to-end commissioning provisioning workflow.
/// </summary>
public sealed class CommissioningExecutionResult
{
    /// <summary>Gets a value indicating whether the commissioning workflow succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Gets a descriptive status or error message from the commissioning workflow.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Gets the provisioned company identifier, if created.</summary>
    public Guid? CompanyId { get; init; }

    /// <summary>Gets the provisioned branch identifier, if created.</summary>
    public Guid? BranchId { get; init; }

    /// <summary>Gets the provisioned administrator user identifier, if created.</summary>
    public Guid? AdminUserId { get; init; }

    /// <summary>Creates a successful commissioning execution result.</summary>
    public static CommissioningExecutionResult Succeeded(Guid? companyId, Guid? branchId, Guid? adminUserId) =>
        new()
        {
            Success = true,
            Message = "Commissioning completed successfully.",
            CompanyId = companyId,
            BranchId = branchId,
            AdminUserId = adminUserId
        };

    /// <summary>Creates a failed commissioning execution result with the specified error message.</summary>
    public static CommissioningExecutionResult Failed(string message) =>
        new()
        {
            Success = false,
            Message = message
        };
}

/// <summary>
/// Service contract for executing the end-to-end commissioning workflow.
/// Keeps WinForms UI forms responsible for UI orchestration and presentation only.
/// </summary>
public interface ICommissioningProvisioningCoordinator
{
    /// <summary>
    /// Executes the first-run commissioning sequence: database secret persistence,
    /// bootstrap container creation (if needed), master data hierarchy provisioning,
    /// administrator provisioning, and commissioning marker creation.
    /// </summary>
    Task<CommissioningExecutionResult> ExecuteCommissioningAsync(
        CommissioningExecutionRequest request,
        IServiceProvider? fallbackServices = null,
        CancellationToken ct = default);
}
