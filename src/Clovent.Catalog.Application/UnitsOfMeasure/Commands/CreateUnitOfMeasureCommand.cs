using Clovent.Catalog.Application.UnitsOfMeasure.Dtos;
using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Catalog.UnitsOfMeasure.ValueObjects;
using MediatR;

namespace Clovent.Catalog.Application.UnitsOfMeasure.Commands;

/// <summary>Creates a new unit of measure catalog entry.</summary>
public sealed record CreateUnitOfMeasureCommand(string Code, string Name) : IRequest<UnitOfMeasureDto>;

/// <summary>Handles <see cref="CreateUnitOfMeasureCommand"/>.</summary>
public sealed class CreateUnitOfMeasureCommandHandler(IUnitOfMeasureRepository repository)
    : IRequestHandler<CreateUnitOfMeasureCommand, UnitOfMeasureDto>
{
    /// <inheritdoc/>
    public async Task<UnitOfMeasureDto> Handle(CreateUnitOfMeasureCommand request, CancellationToken cancellationToken)
    {
        var unit = UnitOfMeasure.Create(UnitOfMeasureCode.Create(request.Code), request.Name);
        await repository.AddAsync(unit, cancellationToken);
        return UnitOfMeasureDto.FromDomain(unit);
    }
}
