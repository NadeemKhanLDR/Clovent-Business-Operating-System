using Clovent.Catalog.Application.UnitsOfMeasure.Dtos;
using Clovent.Catalog.UnitsOfMeasure;
using MediatR;

namespace Clovent.Catalog.Application.UnitsOfMeasure.Commands;

/// <summary>Activates a unit of measure.</summary>
public sealed record ActivateUnitOfMeasureCommand(Guid UnitOfMeasureId) : IRequest<UnitOfMeasureDto>;

/// <summary>Handles <see cref="ActivateUnitOfMeasureCommand"/>.</summary>
public sealed class ActivateUnitOfMeasureCommandHandler(IUnitOfMeasureRepository repository)
    : IRequestHandler<ActivateUnitOfMeasureCommand, UnitOfMeasureDto>
{
    /// <inheritdoc/>
    public async Task<UnitOfMeasureDto> Handle(ActivateUnitOfMeasureCommand request, CancellationToken cancellationToken)
    {
        var unit = await repository.GetByIdAsync(new UnitOfMeasureId(request.UnitOfMeasureId), cancellationToken)
            ?? throw new NotFoundException(nameof(UnitOfMeasure), request.UnitOfMeasureId);

        unit.Activate();
        return UnitOfMeasureDto.FromDomain(unit);
    }
}
