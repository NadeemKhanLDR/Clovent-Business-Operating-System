using Clovent.Catalog.Application.UnitsOfMeasure.Dtos;
using Clovent.Catalog.UnitsOfMeasure;
using MediatR;

namespace Clovent.Catalog.Application.UnitsOfMeasure.Queries;

/// <summary>Retrieves a single unit of measure by identity.</summary>
public sealed record GetUnitOfMeasureByIdQuery(Guid UnitOfMeasureId) : IRequest<UnitOfMeasureDto>;

/// <summary>Handles <see cref="GetUnitOfMeasureByIdQuery"/>.</summary>
public sealed class GetUnitOfMeasureByIdQueryHandler(IUnitOfMeasureRepository repository)
    : IRequestHandler<GetUnitOfMeasureByIdQuery, UnitOfMeasureDto>
{
    /// <inheritdoc/>
    public async Task<UnitOfMeasureDto> Handle(GetUnitOfMeasureByIdQuery request, CancellationToken cancellationToken)
    {
        var unit = await repository.GetByIdAsync(new UnitOfMeasureId(request.UnitOfMeasureId), cancellationToken)
            ?? throw new NotFoundException(nameof(UnitOfMeasure), request.UnitOfMeasureId);

        return UnitOfMeasureDto.FromDomain(unit);
    }
}
