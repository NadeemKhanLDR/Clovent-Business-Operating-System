using System;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Restaurant.Application.CashierAudits.Dtos;
using Clovent.Restaurant.Application.CashierAudits.Services;
using Clovent.Restaurant.Shifts;
using MediatR;

namespace Clovent.Restaurant.Application.CashierAudits.Commands;

/// <summary>Command to trigger an automated cashier audit anomaly scan.</summary>
public sealed record RunCashierAuditAnalysisCommand(
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    Guid? ShiftId = null) : IRequest<CashierAuditAnalysisResultDto>;

/// <summary>Handler for <see cref="RunCashierAuditAnalysisCommand"/>.</summary>
public sealed class RunCashierAuditAnalysisCommandHandler(
    ICashierAuditAnalyzerService analyzerService) : IRequestHandler<RunCashierAuditAnalysisCommand, CashierAuditAnalysisResultDto>
{
    /// <inheritdoc/>
    public async Task<CashierAuditAnalysisResultDto> Handle(
        RunCashierAuditAnalysisCommand request,
        CancellationToken cancellationToken)
    {
        var shiftId = request.ShiftId.HasValue ? new ShiftId(request.ShiftId.Value) : (ShiftId?)null;
        return await analyzerService.RunAnalysisAsync(
            request.FromUtc,
            request.ToUtc,
            shiftId,
            cancellationToken);
    }
}
