using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Clovent.Restaurant.AuditAlerts;

/// <summary>
/// Repository abstraction for durable cashier audit alerts in the Restaurant context.
/// </summary>
public interface ICashierAuditAlertRepository
{
    /// <summary>Finds an alert by its unique aggregate identifier.</summary>
    Task<CashierAuditAlert?> GetByIdAsync(CashierAuditAlertId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all alerts matching optional filtering criteria.</summary>
    Task<IReadOnlyList<CashierAuditAlert>> SearchAsync(
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        string? cashierName = null,
        CashierAnomalyType? anomalyType = null,
        AuditAlertStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>Adds a new alert to durable persistence.</summary>
    Task AddAsync(CashierAuditAlert alert, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing alert.</summary>
    Task UpdateAsync(CashierAuditAlert alert, CancellationToken cancellationToken = default);
}
