using Clovent.Desktop.Configuration;

namespace Clovent.Desktop.Commissioning.Services;

/// <summary>
/// Input parameters collected from the UI to execute first-run system commissioning.
/// </summary>
public sealed class CommissioningExecutionRequest
{
    public required DatabaseConnectionSettings ConnectionSettings { get; init; }
    public required InitialMasterDataProvisioningRequest MasterDataRequest { get; init; }
    public required FirstAdminProvisioningRequest AdminRequest { get; init; }
    public required CommissioningMarker Marker { get; init; }
}

/// <summary>
/// Result of the end-to-end commissioning provisioning workflow.
/// </summary>
public sealed class CommissioningExecutionResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public Guid? CompanyId { get; init; }
    public Guid? BranchId { get; init; }
    public Guid? AdminUserId { get; init; }

    public static CommissioningExecutionResult Succeeded(Guid? companyId, Guid? branchId, Guid? adminUserId) =>
        new()
        {
            Success = true,
            Message = "Commissioning completed successfully.",
            CompanyId = companyId,
            BranchId = branchId,
            AdminUserId = adminUserId
        };

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
