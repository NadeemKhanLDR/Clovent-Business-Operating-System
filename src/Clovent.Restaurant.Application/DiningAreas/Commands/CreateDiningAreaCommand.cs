using Clovent.Identity.Branches;
using Clovent.Restaurant.Application.DiningAreas.Dtos;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.DiningAreas.ValueObjects;
using MediatR;

namespace Clovent.Restaurant.Application.DiningAreas.Commands;

/// <summary>Creates a new dining area under a branch.</summary>
public sealed record CreateDiningAreaCommand(Guid BranchId, string Name) : IRequest<DiningAreaDto>;

/// <summary>Handles <see cref="CreateDiningAreaCommand"/>.</summary>
public sealed class CreateDiningAreaCommandHandler(IDiningAreaRepository repository) : IRequestHandler<CreateDiningAreaCommand, DiningAreaDto>
{
    /// <inheritdoc/>
    public async Task<DiningAreaDto> Handle(CreateDiningAreaCommand request, CancellationToken cancellationToken)
    {
        var area = DiningArea.Create(new BranchId(request.BranchId), DiningAreaName.Create(request.Name));

        await repository.AddAsync(area, cancellationToken);

        return DiningAreaDto.FromDomain(area);
    }
}
