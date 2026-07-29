using Clovent.Restaurant.Application.Payments.Dtos;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using MediatR;

namespace Clovent.Restaurant.Application.Payments.Commands;

/// <summary>
/// Records a payment against an order. Partial payments, multiple tenders,
/// and split-bill scenarios are all a natural consequence of an order
/// accumulating several of these - see <see cref="Payment"/>'s
/// doc comment - so this one command covers all three, not a special
/// "split bill" command.
/// </summary>
public sealed record RecordPaymentCommand(Guid OrderId, Guid PaymentMethodId, decimal Amount) : IRequest<PaymentDto>;

/// <summary>Handles <see cref="RecordPaymentCommand"/>.</summary>
public sealed class RecordPaymentCommandHandler(IOrderRepository orderRepository, IPaymentRepository paymentRepository)
    : IRequestHandler<RecordPaymentCommand, PaymentDto>
{
    /// <inheritdoc/>
    public async Task<PaymentDto> Handle(RecordPaymentCommand request, CancellationToken cancellationToken)
    {
        var orderId = new OrderId(request.OrderId);
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var payment = Payment.Create(orderId, new PaymentMethodId(request.PaymentMethodId), request.Amount);
        order.RecordPayment(payment.Id);

        await paymentRepository.AddAsync(payment, cancellationToken);

        return PaymentDto.FromDomain(payment);
    }
}
