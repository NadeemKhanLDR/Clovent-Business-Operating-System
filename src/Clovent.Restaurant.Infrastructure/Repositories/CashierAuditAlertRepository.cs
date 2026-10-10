using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Restaurant.AuditAlerts;
using Clovent.Restaurant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core repository for <see cref="CashierAuditAlert"/>.</summary>
public sealed class CashierAuditAlertRepository(RestaurantDbContext dbContext) : ICashierAuditAlertRepository
{
    /// <inheritdoc/>
    public async Task<CashierAuditAlert?> GetByIdAsync(CashierAuditAlertId id, CancellationToken cancellationToken = default)
    {
        return await dbContext.CashierAuditAlerts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CashierAuditAlert>> SearchAsync(
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        string? cashierName = null,
        CashierAnomalyType? anomalyType = null,
        AuditAlertStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.CashierAuditAlerts.AsQueryable();

        if (fromUtc.HasValue)
        {
            query = query.Where(a => a.DetectedAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(a => a.DetectedAtUtc <= toUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(cashierName))
        {
            query = query.Where(a => a.CashierName.Contains(cashierName));
        }

        if (anomalyType.HasValue)
        {
            query = query.Where(a => a.AnomalyType == anomalyType.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        return await query
            .OrderByDescending(a => a.DetectedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(CashierAuditAlert alert, CancellationToken cancellationToken = default)
    {
        await dbContext.CashierAuditAlerts.AddAsync(alert, cancellationToken);
    }

    /// <inheritdoc/>
    public Task UpdateAsync(CashierAuditAlert alert, CancellationToken cancellationToken = default)
    {
        dbContext.CashierAuditAlerts.Update(alert);
        return Task.CompletedTask;
    }
}
