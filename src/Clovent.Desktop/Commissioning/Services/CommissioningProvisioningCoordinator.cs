using Clovent.Desktop.Configuration;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Commissioning.Services;

/// <summary>
/// Coordinates the end-to-end provisioning workflow for first-run commissioning.
/// Owns secret storage, bootstrap container lifecycle, master data provisioning,
/// administrator creation, and marker writing.
/// </summary>
public sealed class CommissioningProvisioningCoordinator(
    ICommissioningBootstrapFactory? bootstrapFactory = null,
    IInitialMasterDataProvisioningService? masterDataProvisioning = null,
    IFirstAdminProvisioningService? adminProvisioning = null,
    ILogger<CommissioningProvisioningCoordinator>? logger = null)
    : ICommissioningProvisioningCoordinator
{
    private readonly ICommissioningBootstrapFactory _bootstrapFactory = bootstrapFactory ?? new CommissioningBootstrapFactory();
    private readonly IInitialMasterDataProvisioningService _masterDataProvisioning = masterDataProvisioning ?? new InitialMasterDataProvisioningService();
    private readonly IFirstAdminProvisioningService _adminProvisioning = adminProvisioning ?? new FirstAdminProvisioningService();

    /// <inheritdoc/>
    public async Task<CommissioningExecutionResult> ExecuteCommissioningAsync(
        CommissioningExecutionRequest request,
        IServiceProvider? fallbackServices = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.ConnectionSettings);
        ArgumentNullException.ThrowIfNull(request.MasterDataRequest);
        ArgumentNullException.ThrowIfNull(request.AdminRequest);
        ArgumentNullException.ThrowIfNull(request.Marker);

        // 1. Save Database Connection Settings encrypted with DPAPI
        DatabaseSecretStore.Save(request.ConnectionSettings, machineLevel: true);

        // 2. Resolve ServiceProvider (either injected/fallback or created via bootstrap factory)
        var services = fallbackServices;
        IDisposable? disposableProvider = null;

        if (services == null)
        {
            var connStr = request.ConnectionSettings.BuildConnectionString();
            var localProvider = _bootstrapFactory.CreateBootstrapServiceProvider(connStr);
            services = localProvider;
            disposableProvider = localProvider;
        }

        try
        {
            // 3. Provision Master Data Hierarchy & Settings
            var masterResult = await _masterDataProvisioning.ProvisionInitialMasterDataAsync(
                services,
                request.MasterDataRequest,
                ct).ConfigureAwait(false);

            if (!masterResult.Success)
            {
                return CommissioningExecutionResult.Failed($"Master data provisioning failed: {masterResult.Message}");
            }

            request.AdminRequest.CompanyId = masterResult.CompanyId;
            request.AdminRequest.BranchId = masterResult.BranchId;

            // 4. Provision First Administrator Account
            var (adminSuccess, adminMsg, adminUserId) = await _adminProvisioning.ProvisionFirstAdminAsync(
                services,
                request.AdminRequest,
                ct).ConfigureAwait(false);

            if (!adminSuccess && !adminMsg.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                return CommissioningExecutionResult.Failed($"Administrator provisioning failed: {adminMsg}");
            }

            // 5. Save Commissioning Marker
            CommissioningStateService.SaveMarker(request.Marker);

            logger?.LogInformation("Commissioning finalized successfully for Organization: {Org}, Company: {Company}, Admin: {Admin}",
                request.Marker.OrganizationName, request.Marker.CompanyName, request.Marker.AdminUserName);

            return CommissioningExecutionResult.Succeeded(masterResult.CompanyId, masterResult.BranchId, adminUserId);
        }
        finally
        {
            disposableProvider?.Dispose();
        }
    }
}
