using System;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Restaurant.Application.Shifts.Dtos;
using Clovent.Restaurant.Shifts;
using MediatR;

namespace Clovent.Restaurant.Application.Shifts.Queries;

/// <summary>Query to retrieve a shift by ID.</summary>
public sealed record GetShiftByIdQuery(Guid ShiftId) : IRequest<ShiftDto>;

/// <summary>Handles <see cref="GetShiftByIdQuery"/>.</summary>
public sealed class GetShiftByIdQueryHandler(IShiftRepository shiftRepository) : IRequestHandler<GetShiftByIdQuery, ShiftDto>
{
    /// <inheritdoc/>
    public async Task<ShiftDto> Handle(GetShiftByIdQuery request, CancellationToken cancellationToken)
    {
        var shiftId = new ShiftId(request.ShiftId);
        var shift = await shiftRepository.GetByIdAsync(shiftId, cancellationToken)
            ?? throw new NotFoundException(nameof(Shift), request.ShiftId);

        return ShiftDto.FromDomain(shift);
    }
}
