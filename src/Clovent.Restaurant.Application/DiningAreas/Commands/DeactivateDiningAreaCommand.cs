using Clovent.Restaurant.Application.DiningAreas.Dtos;
using Clovent.Restaurant.DiningAreas;
using MediatR;

namespace Clovent.Restaurant.Application.DiningAreas.Commands;

/// <summary>Deactivates a dining area.</summary>
public sealed record DeactivateDiningAreaCommand(Guid DiningAreaId) : IRequest<DiningAreaDto>;

/// <summary>Handles <see cref="DeactivateDiningAreaCommand"/>.</summary>
public sealed class DeactivateDiningAreaCommandHandler(IDiningAreaRepository repository) : IRequestHandler<DeactivateDiningAreaCommand, DiningAreaDto>
{
    /// <inheritdoc/>
    public async Task<DiningAreaDto> Handle(DeactivateDiningAreaCommand request, CancellationToken cancellationToken)
    {
        var area = await repository.GetByIdAsync(new DiningAreaId(request.DiningAreaId), cancellationToken)
            ?? throw new NotFoundException(nameof(DiningArea), request.DiningAreaId);

        area.Deactivate();
        return DiningAreaDto.FromDomain(area);
    }
}
