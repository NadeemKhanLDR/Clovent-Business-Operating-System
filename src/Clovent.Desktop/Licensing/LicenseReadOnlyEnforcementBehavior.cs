using MediatR;

namespace Clovent.Desktop.Licensing;

/// <summary>
/// Exception thrown when a mutating commercial operation is attempted while the CBOS software license is expired
/// and the application is operating in Read-Only Mode (PDR-0002).
/// </summary>
public sealed class LicenseReadOnlyException(string message) : InvalidOperationException(message);

/// <summary>
/// Enforces PDR-0002 at the MediatR application boundary: when the active license status is
/// <see cref="LicenseStatus.Expired"/>, new commercial mutations (Restaurant orders, payments, refunds, shifts;
/// Inventory movements/adjustments/recipes; Catalog modifications) are blocked, while historical queries,
/// authentication, backups, and diagnostics remain accessible.
/// </summary>
public sealed class LicenseReadOnlyEnforcementBehavior<TRequest, TResponse>(
    Func<LicenseStatus>? statusProvider = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly Func<LicenseStatus> _statusProvider = statusProvider ?? (() => LicenseService.CurrentResult.Status);

    /// <inheritdoc/>
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_statusProvider() == LicenseStatus.Expired && IsBlockedCommercialMutation(typeof(TRequest)))
        {
            throw new LicenseReadOnlyException(
                "Operation blocked: The software license has expired and CBOS is operating in Read-Only Mode (PDR-0002). " +
                "Historical reports, data exports, and database backups remain available.");
        }

        return next();
    }

    internal static bool IsBlockedCommercialMutation(Type requestType)
    {
        var ns = requestType.Namespace ?? string.Empty;
        var name = requestType.Name;

        // Only gate Commands (never Queries, and never Authentication/Identity/MasterData/Backup/Diagnostics).
        if (!name.EndsWith("Command", StringComparison.Ordinal) &&
            !ns.Contains(".Commands", StringComparison.Ordinal))
        {
            return false;
        }

        return ns.StartsWith("Clovent.Restaurant.Application.", StringComparison.Ordinal) ||
               ns.StartsWith("Clovent.Inventory.Application.", StringComparison.Ordinal) ||
               ns.StartsWith("Clovent.Catalog.Application.", StringComparison.Ordinal);
    }
}
