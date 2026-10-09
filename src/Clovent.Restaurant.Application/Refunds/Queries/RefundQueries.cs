using Clovent.Restaurant.Application.Refunds.Dtos;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Refunds;
using MediatR;

namespace Clovent.Restaurant.Application.Refunds.Queries;

/// <summary>Query to retrieve a single refund by its ID.</summary>
public sealed record GetRefundByIdQuery(Guid RefundId) : IRequest<RefundDto?>;

/// <summary>Handles <see cref="GetRefundByIdQuery"/>.</summary>
public sealed class GetRefundByIdQueryHandler(IRefundRepository refundRepository)
    : IRequestHandler<GetRefundByIdQuery, RefundDto?>
{
    /// <inheritdoc/>
    public async Task<RefundDto?> Handle(GetRefundByIdQuery request, CancellationToken cancellationToken)
    {
        var refund = await refundRepository.GetByIdAsync(new RefundId(request.RefundId), cancellationToken);
        return refund != null ? RefundDto.FromDomain(refund) : null;
    }
}

/// <summary>Query to retrieve all refunds posted against an original order.</summary>
public sealed record ListRefundsByOrderQuery(Guid OrderId) : IRequest<IReadOnlyList<RefundDto>>;

/// <summary>Handles <see cref="ListRefundsByOrderQuery"/>.</summary>
public sealed class ListRefundsByOrderQueryHandler(IRefundRepository refundRepository)
    : IRequestHandler<ListRefundsByOrderQuery, IReadOnlyList<RefundDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<RefundDto>> Handle(ListRefundsByOrderQuery request, CancellationToken cancellationToken)
    {
        var refunds = await refundRepository.GetByOrderIdAsync(new OrderId(request.OrderId), cancellationToken);
        return refunds.Select(RefundDto.FromDomain).ToList();
    }
}
