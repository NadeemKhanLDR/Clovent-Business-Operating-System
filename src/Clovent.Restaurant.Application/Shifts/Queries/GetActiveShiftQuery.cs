using System;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Users;
using Clovent.MasterData.Terminals;
using Clovent.Restaurant.Application.Shifts.Dtos;
using Clovent.Restaurant.Shifts;
using MediatR;

namespace Clovent.Restaurant.Application.Shifts.Queries;

/// <summary>Query to find the currently open active shift for a terminal or cashier.</summary>
public sealed record GetActiveShiftQuery(Guid? TerminalId = null, Guid? CashierId = null) : IRequest<ShiftDto?>;

/// <summary>Handles <see cref="GetActiveShiftQuery"/>.</summary>
public sealed class GetActiveShiftQueryHandler(IShiftRepository shiftRepository) : IRequestHandler<GetActiveShiftQuery, ShiftDto?>
{
    /// <inheritdoc/>
    public async Task<ShiftDto?> Handle(GetActiveShiftQuery request, CancellationToken cancellationToken)
    {
        if (request.TerminalId.HasValue)
        {
            var shift = await shiftRepository.GetActiveShiftForTerminalAsync(new TerminalId(request.TerminalId.Value), cancellationToken);
            if (shift != null) return ShiftDto.FromDomain(shift);
        }

        if (request.CashierId.HasValue)
        {
            var shift = await shiftRepository.GetActiveShiftForCashierAsync(new UserId(request.CashierId.Value), cancellationToken);
            if (shift != null) return ShiftDto.FromDomain(shift);
        }

        return null;
    }
}
