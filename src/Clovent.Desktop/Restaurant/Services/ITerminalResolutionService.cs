using System;
using System.Threading;
using System.Threading.Tasks;

namespace Clovent.Desktop.Restaurant.Services;

/// <summary>Result of resolving the physical/logical POS register terminal for the current desktop workstation.</summary>
public sealed record TerminalResolutionResult(
    bool IsConfigured,
    Guid? TerminalId = null,
    string? TerminalName = null,
    string? TerminalCode = null,
    Guid? BranchId = null,
    string? BranchName = null,
    Guid? WarehouseId = null,
    string? WarehouseName = null,
    string? ResolutionSource = null,
    string? ErrorMessage = null);

/// <summary>
/// Service responsible for resolving and binding the canonical Master Data Terminal,
/// Branch, and Warehouse context for the local workstation machine.
/// </summary>
public interface ITerminalResolutionService
{
    /// <summary>Resolves the current workstation's terminal context.</summary>
    Task<TerminalResolutionResult> ResolveCurrentTerminalAsync(CancellationToken cancellationToken = default);
}
