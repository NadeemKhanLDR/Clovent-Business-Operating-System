using Clovent.Restaurant.Application.DiningAreas.Dtos;
using Clovent.Restaurant.DiningAreas;
using MediatR;

namespace Clovent.Restaurant.Application.DiningAreas.Commands;

/// <summary>Activates a dining area.</summary>
public sealed record ActivateDiningAreaCommand(Guid DiningAreaId) : IRequest<DiningAreaDto>;

/// <summary>Handles <see cref="ActivateDiningAreaCommand"/>.</summary>
public sealed class ActivateDiningAreaCommandHandler(IDiningAreaRepository repository) : IRequestHandler<ActivateDiningAreaCommand, DiningAreaDto>
{
    /// <inheritdoc/>
    public async Task<DiningAreaDto> Handle(ActivateDiningAreaCommand request, CancellationToken cancellationToken)
    {
        var area = await repository.GetByIdAsync(new DiningAreaId(request.DiningAreaId), cancellationToken)
            ?? throw new NotFoundException(nameof(DiningArea), request.DiningAreaId);

        area.Activate();
        return DiningAreaDto.FromDomain(area);
    }
}
