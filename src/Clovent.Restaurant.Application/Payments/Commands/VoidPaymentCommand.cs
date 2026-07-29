using Clovent.Restaurant.Application.Payments.Dtos;
using Clovent.Restaurant.Payments;
using MediatR;

namespace Clovent.Restaurant.Application.Payments.Commands;

/// <summary>Voids a payment recorded in error. One-way - a correction is a new payment, not an unvoid.</summary>
public sealed record VoidPaymentCommand(Guid PaymentId) : IRequest<PaymentDto>;

/// <summary>Handles <see cref="VoidPaymentCommand"/>.</summary>
public sealed class VoidPaymentCommandHandler(IPaymentRepository repository) : IRequestHandler<VoidPaymentCommand, PaymentDto>
{
    /// <inheritdoc/>
    public async Task<PaymentDto> Handle(VoidPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await repository.GetByIdAsync(new PaymentId(request.PaymentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), request.PaymentId);

        payment.Void();
        return PaymentDto.FromDomain(payment);
    }
}
