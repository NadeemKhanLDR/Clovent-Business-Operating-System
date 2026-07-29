using Clovent.Restaurant.Application.Payments.Dtos;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Payments;
using MediatR;

namespace Clovent.Restaurant.Application.Payments.Queries;

/// <summary>Retrieves every payment recorded against an order.</summary>
public sealed record ListPaymentsByOrderQuery(Guid OrderId) : IRequest<IReadOnlyCollection<PaymentDto>>;

/// <summary>Handles <see cref="ListPaymentsByOrderQuery"/>.</summary>
public sealed class ListPaymentsByOrderQueryHandler(IPaymentRepository repository)
    : IRequestHandler<ListPaymentsByOrderQuery, IReadOnlyCollection<PaymentDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<PaymentDto>> Handle(ListPaymentsByOrderQuery request, CancellationToken cancellationToken)
    {
        var payments = await repository.GetByOrderIdAsync(new OrderId(request.OrderId), cancellationToken);
        return [.. payments.Select(PaymentDto.FromDomain)];
    }
}
