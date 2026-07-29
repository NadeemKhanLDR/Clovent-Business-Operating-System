using Clovent.Restaurant.Application.ServiceCharges.Dtos;
using Clovent.Restaurant.ServiceCharges;
using MediatR;

namespace Clovent.Restaurant.Application.ServiceCharges.Queries;

/// <summary>Retrieves a service charge by id.</summary>
public sealed record GetServiceChargeByIdQuery(Guid ServiceChargeId) : IRequest<ServiceChargeDto>;

/// <summary>Handles <see cref="GetServiceChargeByIdQuery"/>.</summary>
public sealed class GetServiceChargeByIdQueryHandler(IServiceChargeRepository repository) : IRequestHandler<GetServiceChargeByIdQuery, ServiceChargeDto>
{
    /// <inheritdoc/>
    public async Task<ServiceChargeDto> Handle(GetServiceChargeByIdQuery request, CancellationToken cancellationToken)
    {
        var charge = await repository.GetByIdAsync(new ServiceChargeId(request.ServiceChargeId), cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceCharge), request.ServiceChargeId);

        return ServiceChargeDto.FromDomain(charge);
    }
}
