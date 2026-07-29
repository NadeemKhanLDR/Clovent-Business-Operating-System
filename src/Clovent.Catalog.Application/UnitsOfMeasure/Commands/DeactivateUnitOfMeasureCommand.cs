using Clovent.Catalog.Application.UnitsOfMeasure.Dtos;
using Clovent.Catalog.UnitsOfMeasure;
using MediatR;

namespace Clovent.Catalog.Application.UnitsOfMeasure.Commands;

/// <summary>Deactivates a unit of measure.</summary>
public sealed record DeactivateUnitOfMeasureCommand(Guid UnitOfMeasureId) : IRequest<UnitOfMeasureDto>;

/// <summary>Handles <see cref="DeactivateUnitOfMeasureCommand"/>.</summary>
public sealed class DeactivateUnitOfMeasureCommandHandler(IUnitOfMeasureRepository repository)
    : IRequestHandler<DeactivateUnitOfMeasureCommand, UnitOfMeasureDto>
{
    /// <inheritdoc/>
    public async Task<UnitOfMeasureDto> Handle(DeactivateUnitOfMeasureCommand request, CancellationToken cancellationToken)
    {
        var unit = await repository.GetByIdAsync(new UnitOfMeasureId(request.UnitOfMeasureId), cancellationToken)
            ?? throw new NotFoundException(nameof(UnitOfMeasure), request.UnitOfMeasureId);

        unit.Deactivate();
        return UnitOfMeasureDto.FromDomain(unit);
    }
}
