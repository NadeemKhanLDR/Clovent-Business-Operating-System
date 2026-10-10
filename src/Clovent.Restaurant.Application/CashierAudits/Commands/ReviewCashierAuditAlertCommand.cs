using System;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Restaurant.Application.CashierAudits.Dtos;
using Clovent.Restaurant.AuditAlerts;
using MediatR;

namespace Clovent.Restaurant.Application.CashierAudits.Commands;

/// <summary>Command for a manager to review, resolve, or dismiss a cashier audit alert.</summary>
public sealed record ReviewCashierAuditAlertCommand(
    Guid AlertId,
    string ReviewedBy,
    string ResolutionNotes,
    bool Dismiss = false) : IRequest<CashierAuditAlertDto>;

/// <summary>Handler for <see cref="ReviewCashierAuditAlertCommand"/>.</summary>
public sealed class ReviewCashierAuditAlertCommandHandler(
    ICashierAuditAlertRepository alertRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ReviewCashierAuditAlertCommand, CashierAuditAlertDto>
{
    /// <inheritdoc/>
    public async Task<CashierAuditAlertDto> Handle(
        ReviewCashierAuditAlertCommand request,
        CancellationToken cancellationToken)
    {
        var alert = await alertRepository.GetByIdAsync(new CashierAuditAlertId(request.AlertId), cancellationToken)
            ?? throw new NotFoundException(nameof(CashierAuditAlert), request.AlertId);

        if (request.Dismiss)
        {
            alert.Dismiss(request.ReviewedBy, request.ResolutionNotes);
        }
        else
        {
            alert.Review(request.ReviewedBy, request.ResolutionNotes);
        }

        await alertRepository.UpdateAsync(alert, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CashierAuditAlertDto.FromDomain(alert);
    }
}
