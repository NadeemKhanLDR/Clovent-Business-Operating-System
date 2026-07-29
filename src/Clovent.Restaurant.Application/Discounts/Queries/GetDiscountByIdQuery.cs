using Clovent.Restaurant.Application.Discounts.Dtos;
using Clovent.Restaurant.Discounts;
using MediatR;

namespace Clovent.Restaurant.Application.Discounts.Queries;

/// <summary>Retrieves a discount by id.</summary>
public sealed record GetDiscountByIdQuery(Guid DiscountId) : IRequest<DiscountDto>;

/// <summary>Handles <see cref="GetDiscountByIdQuery"/>.</summary>
public sealed class GetDiscountByIdQueryHandler(IDiscountRepository repository) : IRequestHandler<GetDiscountByIdQuery, DiscountDto>
{
    /// <inheritdoc/>
    public async Task<DiscountDto> Handle(GetDiscountByIdQuery request, CancellationToken cancellationToken)
    {
        var discount = await repository.GetByIdAsync(new DiscountId(request.DiscountId), cancellationToken)
            ?? throw new NotFoundException(nameof(Discount), request.DiscountId);

        return DiscountDto.FromDomain(discount);
    }
}
