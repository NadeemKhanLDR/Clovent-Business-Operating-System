using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Restaurant.AuditAlerts;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class FakeCashierAuditAlertRepository : ICashierAuditAlertRepository
{
    private readonly Dictionary<CashierAuditAlertId, CashierAuditAlert> _alerts = [];

    public IReadOnlyCollection<CashierAuditAlert> Items => _alerts.Values;

    public Task<CashierAuditAlert?> GetByIdAsync(CashierAuditAlertId id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_alerts.GetValueOrDefault(id));
    }

    public Task<IReadOnlyList<CashierAuditAlert>> SearchAsync(
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        string? cashierName = null,
        CashierAnomalyType? anomalyType = null,
        AuditAlertStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _alerts.Values.AsEnumerable();

        if (fromUtc.HasValue)
            query = query.Where(a => a.DetectedAtUtc >= fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(a => a.DetectedAtUtc <= toUtc.Value);

        if (!string.IsNullOrWhiteSpace(cashierName))
            query = query.Where(a => a.CashierName.Contains(cashierName, StringComparison.OrdinalIgnoreCase));

        if (anomalyType.HasValue)
            query = query.Where(a => a.AnomalyType == anomalyType.Value);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        return Task.FromResult<IReadOnlyList<CashierAuditAlert>>(query.OrderByDescending(a => a.DetectedAtUtc).ToList());
    }

    public Task AddAsync(CashierAuditAlert alert, CancellationToken cancellationToken = default)
    {
        _alerts[alert.Id] = alert;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(CashierAuditAlert alert, CancellationToken cancellationToken = default)
    {
        _alerts[alert.Id] = alert;
        return Task.CompletedTask;
    }
}
