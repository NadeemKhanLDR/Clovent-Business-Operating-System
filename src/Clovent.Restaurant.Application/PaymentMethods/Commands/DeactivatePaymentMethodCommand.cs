using Clovent.Restaurant.Application.PaymentMethods.Dtos;
using Clovent.Restaurant.PaymentMethods;
using MediatR;

namespace Clovent.Restaurant.Application.PaymentMethods.Commands;

/// <summary>Deactivates a payment method.</summary>
public sealed record DeactivatePaymentMethodCommand(Guid PaymentMethodId) : IRequest<PaymentMethodDto>;

/// <summary>Handles <see cref="DeactivatePaymentMethodCommand"/>.</summary>
public sealed class DeactivatePaymentMethodCommandHandler(IPaymentMethodRepository repository) : IRequestHandler<DeactivatePaymentMethodCommand, PaymentMethodDto>
{
    /// <inheritdoc/>
    public async Task<PaymentMethodDto> Handle(DeactivatePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var method = await repository.GetByIdAsync(new PaymentMethodId(request.PaymentMethodId), cancellationToken)
            ?? throw new NotFoundException(nameof(PaymentMethod), request.PaymentMethodId);

        method.Deactivate();
        return PaymentMethodDto.FromDomain(method);
    }
}
