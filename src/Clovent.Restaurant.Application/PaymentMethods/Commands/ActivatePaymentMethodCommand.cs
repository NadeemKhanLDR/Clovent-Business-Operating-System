using Clovent.Restaurant.Application.PaymentMethods.Dtos;
using Clovent.Restaurant.PaymentMethods;
using MediatR;

namespace Clovent.Restaurant.Application.PaymentMethods.Commands;

/// <summary>Activates a payment method.</summary>
public sealed record ActivatePaymentMethodCommand(Guid PaymentMethodId) : IRequest<PaymentMethodDto>;

/// <summary>Handles <see cref="ActivatePaymentMethodCommand"/>.</summary>
public sealed class ActivatePaymentMethodCommandHandler(IPaymentMethodRepository repository) : IRequestHandler<ActivatePaymentMethodCommand, PaymentMethodDto>
{
    /// <inheritdoc/>
    public async Task<PaymentMethodDto> Handle(ActivatePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var method = await repository.GetByIdAsync(new PaymentMethodId(request.PaymentMethodId), cancellationToken)
            ?? throw new NotFoundException(nameof(PaymentMethod), request.PaymentMethodId);

        method.Activate();
        return PaymentMethodDto.FromDomain(method);
    }
}
