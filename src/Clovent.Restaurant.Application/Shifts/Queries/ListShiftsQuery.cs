using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Users;
using Clovent.MasterData.Terminals;
using Clovent.Restaurant.Application.Shifts.Dtos;
using Clovent.Restaurant.Shifts;
using MediatR;

namespace Clovent.Restaurant.Application.Shifts.Queries;

/// <summary>Query to search/filter shifts for Shift History.</summary>
public sealed record ListShiftsQuery(
    Guid? TerminalId = null,
    Guid? CashierId = null,
    ShiftStatus? Status = null,
    DateTimeOffset? FromDateUtc = null,
    DateTimeOffset? ToDateUtc = null) : IRequest<IReadOnlyList<ShiftDto>>;

/// <summary>Handles <see cref="ListShiftsQuery"/>.</summary>
public sealed class ListShiftsQueryHandler(IShiftRepository shiftRepository) : IRequestHandler<ListShiftsQuery, IReadOnlyList<ShiftDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<ShiftDto>> Handle(ListShiftsQuery request, CancellationToken cancellationToken)
    {
        TerminalId? terminalId = request.TerminalId.HasValue ? new TerminalId(request.TerminalId.Value) : null;
        UserId? cashierId = request.CashierId.HasValue ? new UserId(request.CashierId.Value) : null;

        var shifts = await shiftRepository.SearchShiftsAsync(
            terminalId,
            cashierId,
            request.Status,
            request.FromDateUtc,
            request.ToDateUtc,
            cancellationToken);

        return shifts.Select(ShiftDto.FromDomain).ToList();
    }
}
