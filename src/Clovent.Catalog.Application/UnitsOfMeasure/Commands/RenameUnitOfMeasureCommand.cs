using Clovent.Catalog.Application.UnitsOfMeasure.Dtos;
using Clovent.Catalog.UnitsOfMeasure;
using MediatR;

namespace Clovent.Catalog.Application.UnitsOfMeasure.Commands;

/// <summary>Renames an existing unit of measure.</summary>
public sealed record RenameUnitOfMeasureCommand(Guid UnitOfMeasureId, string Name) : IRequest<UnitOfMeasureDto>;

/// <summary>Handles <see cref="RenameUnitOfMeasureCommand"/>.</summary>
public sealed class RenameUnitOfMeasureCommandHandler(IUnitOfMeasureRepository repository)
    : IRequestHandler<RenameUnitOfMeasureCommand, UnitOfMeasureDto>
{
    /// <inheritdoc/>
    public async Task<UnitOfMeasureDto> Handle(RenameUnitOfMeasureCommand request, CancellationToken cancellationToken)
    {
        var unit = await repository.GetByIdAsync(new UnitOfMeasureId(request.UnitOfMeasureId), cancellationToken)
            ?? throw new NotFoundException(nameof(UnitOfMeasure), request.UnitOfMeasureId);

        unit.Rename(request.Name);
        return UnitOfMeasureDto.FromDomain(unit);
    }
}
