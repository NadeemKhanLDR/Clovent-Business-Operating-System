using Clovent.Restaurant.Application.DiningAreas.Dtos;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.DiningAreas.ValueObjects;
using MediatR;

namespace Clovent.Restaurant.Application.DiningAreas.Commands;

/// <summary>Renames a dining area.</summary>
public sealed record RenameDiningAreaCommand(Guid DiningAreaId, string Name) : IRequest<DiningAreaDto>;

/// <summary>Handles <see cref="RenameDiningAreaCommand"/>.</summary>
public sealed class RenameDiningAreaCommandHandler(IDiningAreaRepository repository) : IRequestHandler<RenameDiningAreaCommand, DiningAreaDto>
{
    /// <inheritdoc/>
    public async Task<DiningAreaDto> Handle(RenameDiningAreaCommand request, CancellationToken cancellationToken)
    {
        var area = await repository.GetByIdAsync(new DiningAreaId(request.DiningAreaId), cancellationToken)
            ?? throw new NotFoundException(nameof(DiningArea), request.DiningAreaId);

        area.Rename(DiningAreaName.Create(request.Name));
        return DiningAreaDto.FromDomain(area);
    }
}
