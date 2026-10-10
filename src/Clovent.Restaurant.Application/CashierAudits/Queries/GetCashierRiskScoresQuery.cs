using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Restaurant.Application.CashierAudits.Dtos;
using Clovent.Restaurant.Application.CashierAudits.Services;
using MediatR;

namespace Clovent.Restaurant.Application.CashierAudits.Queries;

/// <summary>Query to retrieve cashier risk rankings, composite scores, and sparkline trends.</summary>
public sealed record GetCashierRiskScoresQuery(
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null) : IRequest<IReadOnlyList<CashierRiskScoreDto>>;

/// <summary>Handler for <see cref="GetCashierRiskScoresQuery"/>.</summary>
public sealed class GetCashierRiskScoresQueryHandler(
    ICashierAuditAnalyzerService analyzerService) : IRequestHandler<GetCashierRiskScoresQuery, IReadOnlyList<CashierRiskScoreDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<CashierRiskScoreDto>> Handle(
        GetCashierRiskScoresQuery request,
        CancellationToken cancellationToken)
    {
        return await analyzerService.ComputeCashierRiskScoresAsync(
            request.FromUtc,
            request.ToUtc,
            cancellationToken);
    }
}
