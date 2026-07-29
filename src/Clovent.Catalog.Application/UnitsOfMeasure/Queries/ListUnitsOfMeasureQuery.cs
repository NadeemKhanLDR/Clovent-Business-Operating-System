using Clovent.Catalog.Application.UnitsOfMeasure.Dtos;
using Clovent.Catalog.UnitsOfMeasure;
using MediatR;

namespace Clovent.Catalog.Application.UnitsOfMeasure.Queries;

/// <summary>Retrieves every unit of measure.</summary>
public sealed record ListUnitsOfMeasureQuery : IRequest<IReadOnlyCollection<UnitOfMeasureDto>>;

/// <summary>Handles <see cref="ListUnitsOfMeasureQuery"/>.</summary>
public sealed class ListUnitsOfMeasureQueryHandler(IUnitOfMeasureRepository repository)
    : IRequestHandler<ListUnitsOfMeasureQuery, IReadOnlyCollection<UnitOfMeasureDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<UnitOfMeasureDto>> Handle(ListUnitsOfMeasureQuery request, CancellationToken cancellationToken)
    {
        var units = await repository.GetAllAsync(cancellationToken);
        return [.. units.Select(UnitOfMeasureDto.FromDomain)];
    }
}
