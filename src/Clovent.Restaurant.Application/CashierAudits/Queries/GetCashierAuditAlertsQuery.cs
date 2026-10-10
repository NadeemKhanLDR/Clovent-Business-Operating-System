using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Restaurant.Application.CashierAudits.Dtos;
using Clovent.Restaurant.AuditAlerts;
using MediatR;

namespace Clovent.Restaurant.Application.CashierAudits.Queries;

/// <summary>Query to retrieve persisted cashier audit alerts.</summary>
public sealed record GetCashierAuditAlertsQuery(
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    string? CashierName = null,
    CashierAnomalyType? AnomalyType = null,
    AuditAlertStatus? Status = null) : IRequest<IReadOnlyList<CashierAuditAlertDto>>;

/// <summary>Handler for <see cref="GetCashierAuditAlertsQuery"/>.</summary>
public sealed class GetCashierAuditAlertsQueryHandler(
    ICashierAuditAlertRepository alertRepository) : IRequestHandler<GetCashierAuditAlertsQuery, IReadOnlyList<CashierAuditAlertDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<CashierAuditAlertDto>> Handle(
        GetCashierAuditAlertsQuery request,
        CancellationToken cancellationToken)
    {
        var alerts = await alertRepository.SearchAsync(
            request.FromUtc,
            request.ToUtc,
            request.CashierName,
            request.AnomalyType,
            request.Status,
            cancellationToken);

        return alerts
            .OrderByDescending(a => a.DetectedAtUtc)
            .Select(CashierAuditAlertDto.FromDomain)
            .ToList();
    }
}
