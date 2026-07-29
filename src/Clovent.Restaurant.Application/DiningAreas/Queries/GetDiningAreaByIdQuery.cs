using Clovent.Restaurant.Application.DiningAreas.Dtos;
using Clovent.Restaurant.DiningAreas;
using MediatR;

namespace Clovent.Restaurant.Application.DiningAreas.Queries;

/// <summary>Retrieves a dining area by id.</summary>
public sealed record GetDiningAreaByIdQuery(Guid DiningAreaId) : IRequest<DiningAreaDto>;

/// <summary>Handles <see cref="GetDiningAreaByIdQuery"/>.</summary>
public sealed class GetDiningAreaByIdQueryHandler(IDiningAreaRepository repository) : IRequestHandler<GetDiningAreaByIdQuery, DiningAreaDto>
{
    /// <inheritdoc/>
    public async Task<DiningAreaDto> Handle(GetDiningAreaByIdQuery request, CancellationToken cancellationToken)
    {
        var area = await repository.GetByIdAsync(new DiningAreaId(request.DiningAreaId), cancellationToken)
            ?? throw new NotFoundException(nameof(DiningArea), request.DiningAreaId);

        return DiningAreaDto.FromDomain(area);
    }
}
