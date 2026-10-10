using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Restaurant.Application.CashierAudits.Dtos;
using Clovent.Restaurant.AuditAlerts;
using Clovent.Restaurant.Shifts;

namespace Clovent.Restaurant.Application.CashierAudits.Services;

/// <summary>
/// Service contract for scanning shifts and transactions for high-risk cashier behavioral anomalies,
/// fraud patterns, excessive voids after cash tenders, unauthorized overrides, and shrinkage indicators.
/// </summary>
public interface ICashierAuditAnalyzerService
{
    /// <summary>
    /// Executes an automated audit scan over shifts and orders within a specified time range or for a specific shift.
    /// Newly surfaced anomalies are persisted to <c>CashierAuditAlerts</c> and trigger outbox notifications.
    /// </summary>
    Task<CashierAuditAnalysisResultDto> RunAnalysisAsync(
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        ShiftId? specificShiftId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes aggregated risk profiles, scores, and sparklines for each cashier based on historical audit events.
    /// </summary>
    Task<IReadOnlyList<CashierRiskScoreDto>> ComputeCashierRiskScoresAsync(
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        CancellationToken cancellationToken = default);
}
