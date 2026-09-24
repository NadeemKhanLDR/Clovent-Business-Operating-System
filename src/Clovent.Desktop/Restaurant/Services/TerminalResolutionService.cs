using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Branches;
using Clovent.Identity.Companies;
using Clovent.Identity.Organizations;
using Clovent.Identity.Users;
using Clovent.MasterData.Shared;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Warehouses;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Restaurant.Services;

/// <summary>
/// Resolves the workstation's canonical Terminal, Branch, and Warehouse context.
/// Evaluates in strict precedence:
/// 1. CBOS_TERMINAL_ID environment variable (Guid, Code, or Name)
/// 2. Persisted workstation settings (PosSettingsStore) validated against Master Data
/// 3. Workstation hostname (Environment.MachineName) matching registered terminal Code or Name
/// 4. Sole active terminal for the resolved branch
/// 5. Deterministic fallback to first active terminal (ordered by Code)
/// </summary>
public sealed class TerminalResolutionService(
    ITerminalRepository terminalRepository,
    IBranchRepository branchRepository,
    IWarehouseRepository warehouseRepository,
    IUserRepository userRepository,
    ICompanyRepository companyRepository,
    IOrganizationRepository organizationRepository,
    ICurrentSession currentSession,
    ILogger<TerminalResolutionService> logger) : ITerminalResolutionService
{
    /// <inheritdoc/>
    public async Task<TerminalResolutionResult> ResolveCurrentTerminalAsync(CancellationToken cancellationToken = default)
    {
        // Step 1: Resolve target branch context
        var targetBranchId = await ResolveTargetBranchIdAsync(cancellationToken);
        if (targetBranchId == null)
        {
            logger.LogWarning("Terminal resolution failed: No restaurant branch could be identified.");
            return new TerminalResolutionResult(
                IsConfigured: false,
                ErrorMessage: "No restaurant branch is configured in the system. Please set up a branch in Master Data.");
        }

        var resolvedBranch = await branchRepository.GetByIdAsync(targetBranchId.Value, cancellationToken);
        var branchName = resolvedBranch?.Name.Value ?? "Default Branch";

        // Step 2: Load and sort active terminals for this branch deterministically
        var branchTerminals = await terminalRepository.GetByBranchIdAsync(targetBranchId.Value, cancellationToken);
        var activeTerminals = branchTerminals
            .Where(t => t.Status == MasterDataStatus.Active)
            .OrderBy(t => t.Code.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Step 3: Evaluate Precedence Order

        // 3.1. Environment Variable CBOS_TERMINAL_ID
        var envVal = Environment.GetEnvironmentVariable("CBOS_TERMINAL_ID")?.Trim();
        if (!string.IsNullOrWhiteSpace(envVal))
        {
            Terminal? envMatched = null;
            if (Guid.TryParse(envVal, out var envGuid))
            {
                envMatched = activeTerminals.FirstOrDefault(t => t.Id.Value == envGuid)
                    ?? await terminalRepository.GetByIdAsync(new TerminalId(envGuid), cancellationToken);
            }

            if (envMatched == null || envMatched.Status != MasterDataStatus.Active)
            {
                envMatched = activeTerminals.FirstOrDefault(t => string.Equals(t.Code.Value, envVal, StringComparison.OrdinalIgnoreCase))
                    ?? activeTerminals.FirstOrDefault(t => string.Equals(t.Name.Value, envVal, StringComparison.OrdinalIgnoreCase));
            }

            if (envMatched != null && envMatched.Status == MasterDataStatus.Active)
            {
                logger.LogInformation("Terminal resolved from CBOS_TERMINAL_ID='{EnvVal}': {Name} ({Code}) [ID: {Id}]",
                    envVal, envMatched.Name.Value, envMatched.Code.Value, envMatched.Id.Value);

                PosSettingsStore.SaveTerminalId(envMatched.Id.Value);
                PosSettingsStore.SaveBranchId(envMatched.BranchId.Value);

                return await BuildResultAsync(envMatched, resolvedBranch, targetBranchId.Value, "EnvironmentVariable", cancellationToken);
            }
            else
            {
                logger.LogWarning("CBOS_TERMINAL_ID='{EnvVal}' did not match any active terminal.", envVal);
            }
        }

        // 3.2. Persisted Workstation Settings (PosSettingsStore)
        var savedTerminalId = PosSettingsStore.LoadTerminalId();
        if (savedTerminalId.HasValue && savedTerminalId.Value != Guid.Empty)
        {
            var savedMatched = await terminalRepository.GetByIdAsync(new TerminalId(savedTerminalId.Value), cancellationToken);
            if (savedMatched != null && savedMatched.Status == MasterDataStatus.Active)
            {
                logger.LogInformation("Terminal resolved from saved workstation settings: {Name} ({Code}) [ID: {Id}]",
                    savedMatched.Name.Value, savedMatched.Code.Value, savedMatched.Id.Value);

                return await BuildResultAsync(savedMatched, resolvedBranch, savedMatched.BranchId, "SavedSettings", cancellationToken);
            }
            else
            {
                logger.LogWarning("Saved TerminalId '{SavedId}' is invalid or inactive; clearing stale setting.", savedTerminalId.Value);
                PosSettingsStore.SaveTerminalId(null);
            }
        }

        // 3.3. Host Machine Name (Environment.MachineName)
        var machineName = Environment.MachineName?.Trim();
        if (!string.IsNullOrWhiteSpace(machineName))
        {
            var machineMatched = activeTerminals.FirstOrDefault(t => string.Equals(t.Code.Value, machineName, StringComparison.OrdinalIgnoreCase))
                ?? activeTerminals.FirstOrDefault(t => string.Equals(t.Name.Value, machineName, StringComparison.OrdinalIgnoreCase));

            if (machineMatched != null)
            {
                logger.LogInformation("Terminal resolved from machine name '{MachineName}': {Name} ({Code}) [ID: {Id}]",
                    machineName, machineMatched.Name.Value, machineMatched.Code.Value, machineMatched.Id.Value);

                PosSettingsStore.SaveTerminalId(machineMatched.Id.Value);
                PosSettingsStore.SaveBranchId(machineMatched.BranchId.Value);

                return await BuildResultAsync(machineMatched, resolvedBranch, targetBranchId.Value, "MachineName", cancellationToken);
            }
        }

        // 3.4. Single Active Terminal on Branch (Auto-bind)
        if (activeTerminals.Count == 1)
        {
            var autoTerminal = activeTerminals[0];
            logger.LogInformation("Terminal resolved as single active terminal on branch '{BranchName}': {Name} ({Code}) [ID: {Id}]",
                branchName, autoTerminal.Name.Value, autoTerminal.Code.Value, autoTerminal.Id.Value);

            PosSettingsStore.SaveTerminalId(autoTerminal.Id.Value);
            PosSettingsStore.SaveBranchId(targetBranchId.Value.Value);

            return await BuildResultAsync(autoTerminal, resolvedBranch, targetBranchId.Value, "SingleBranchTerminal", cancellationToken);
        }

        // 3.5. Multiple Active Terminals on Branch (Ambiguous - Explicit Selection/Configuration Required)
        if (activeTerminals.Count > 1)
        {
            logger.LogWarning("Multiple active terminals ({Count}) found on branch '{BranchName}', but no explicit workstation binding matched. Explicit terminal selection or configuration is required.",
                activeTerminals.Count, branchName);

            return new TerminalResolutionResult(
                IsConfigured: false,
                BranchId: targetBranchId.Value.Value,
                BranchName: branchName,
                ErrorMessage: $"Multiple active terminals ({activeTerminals.Count}) found for branch '{branchName}'. Workstation terminal selection or configuration is required.");
        }

        // 3.6. Zero Active Terminals on Branch
        logger.LogWarning("No active POS terminal found for branch '{BranchName}'.", branchName);
        return new TerminalResolutionResult(
            IsConfigured: false,
            ErrorMessage: $"No active POS terminal found for branch '{branchName}'. Please configure a terminal in Master Data > Terminals.");
    }

    private async Task<BranchId?> ResolveTargetBranchIdAsync(CancellationToken cancellationToken)
    {
        // 1. Saved terminal's branch
        var savedTerminalId = PosSettingsStore.LoadTerminalId();
        if (savedTerminalId.HasValue && savedTerminalId.Value != Guid.Empty)
        {
            var term = await terminalRepository.GetByIdAsync(new TerminalId(savedTerminalId.Value), cancellationToken);
            if (term != null && term.Status == MasterDataStatus.Active)
            {
                return term.BranchId;
            }
        }

        // 2. User's explicit branch
        if (currentSession.UserId.HasValue)
        {
            var user = await userRepository.GetByIdAsync(new UserId(currentSession.UserId.Value), cancellationToken);
            if (user?.BranchId != null)
            {
                return user.BranchId;
            }

            if (user?.CompanyId != null)
            {
                var companyBranches = await branchRepository.GetByCompanyIdAsync(user.CompanyId.Value, cancellationToken);
                var activeBranch = companyBranches.FirstOrDefault(b => b.Status == BranchStatus.Active) ?? companyBranches.FirstOrDefault();
                if (activeBranch != null)
                {
                    return activeBranch.Id;
                }
            }
        }

        // 3. PosSettingsStore saved branch
        var savedBranchId = PosSettingsStore.LoadBranchId();
        if (savedBranchId.HasValue && savedBranchId.Value != Guid.Empty)
        {
            return new BranchId(savedBranchId.Value);
        }

        // 4. Fallback to first active branch in organization/companies
        var orgs = await organizationRepository.GetAllAsync(cancellationToken);
        foreach (var org in orgs)
        {
            var companies = await companyRepository.GetByOrganizationIdAsync(org.Id, cancellationToken);
            foreach (var comp in companies)
            {
                var branches = await branchRepository.GetByCompanyIdAsync(comp.Id, cancellationToken);
                var active = branches.FirstOrDefault(b => b.Status == BranchStatus.Active) ?? branches.FirstOrDefault();
                if (active != null)
                {
                    return active.Id;
                }
            }
        }

        return null;
    }

    private async Task<TerminalResolutionResult> BuildResultAsync(
        Terminal terminal,
        Branch? branch,
        BranchId branchId,
        string source,
        CancellationToken cancellationToken)
    {
        var resolvedBranch = branch ?? await branchRepository.GetByIdAsync(branchId, cancellationToken);
        var branchWarehouses = await warehouseRepository.GetByBranchIdAsync(branchId, cancellationToken);
        var activeWh = branchWarehouses.FirstOrDefault(w => w.Status == MasterDataStatus.Active) ?? branchWarehouses.FirstOrDefault();

        return new TerminalResolutionResult(
            IsConfigured: true,
            TerminalId: terminal.Id.Value,
            TerminalName: terminal.Name.Value,
            TerminalCode: terminal.Code.Value,
            BranchId: branchId.Value,
            BranchName: resolvedBranch?.Name.Value ?? "Default Branch",
            WarehouseId: activeWh?.Id.Value,
            WarehouseName: activeWh?.Name.Value ?? "Main Warehouse",
            ResolutionSource: source);
    }
}
