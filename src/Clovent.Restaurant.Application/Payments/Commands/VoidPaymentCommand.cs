using Clovent.Restaurant.Application.Payments.Dtos;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.PaymentMethods;
using MediatR;

namespace Clovent.Restaurant.Application.Payments.Commands;

/// <summary>Voids a payment recorded in error. One-way - a correction is a new payment, not an unvoid.</summary>
public sealed record VoidPaymentCommand(Guid PaymentId) : IRequest<PaymentDto>;

/// <summary>Handles <see cref="VoidPaymentCommand"/>.</summary>
public sealed class VoidPaymentCommandHandler(
    IPaymentRepository repository,
    IOrderRepository orderRepository,
    ICustomerRepository customerRepository,
    ICustomerLedgerEntryRepository ledgerRepository,
    IPaymentMethodRepository paymentMethodRepository) : IRequestHandler<VoidPaymentCommand, PaymentDto>
{
    /// <inheritdoc/>
    public async Task<PaymentDto> Handle(VoidPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await repository.GetByIdAsync(new PaymentId(request.PaymentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), request.PaymentId);

        if (payment.IsVoided)
        {
            throw new InvalidOperationException("Payment is already voided.");
        }

        var paymentMethod = await paymentMethodRepository.GetByIdAsync(payment.PaymentMethodId, cancellationToken);
        if (paymentMethod is not null)
        {
            var isCredit = string.Equals(paymentMethod.Name.Value, "Credit", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(paymentMethod.Name.Value, "Customer Credit", StringComparison.OrdinalIgnoreCase);

            if (isCredit)
            {
                var order = await orderRepository.GetByIdAsync(payment.OrderId, cancellationToken);
                if (order?.CustomerId is { } customerId)
                {
                    var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken);
                    if (customer is not null)
                    {
                        customer.AdjustBalance(-payment.Amount);
                        await customerRepository.UpdateAsync(customer, cancellationToken);

                        var ledgerEntry = CustomerLedgerEntry.Create(
                            customer.Id,
                            $"VOID-{payment.Id.Value}",
                            $"Void Credit Sale ({order.OrderNumber.Value})",
                            0m,
                            payment.Amount,
                            customer.OutstandingBalance);
                        await ledgerRepository.AddAsync(ledgerEntry, cancellationToken);
                    }
                }
            }
        }

        payment.Void();
        return PaymentDto.FromDomain(payment);
    }
}
