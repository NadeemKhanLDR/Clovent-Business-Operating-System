using Clovent.Restaurant.Application.PaymentMethods.Dtos;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using MediatR;

namespace Clovent.Restaurant.Application.PaymentMethods.Commands;

/// <summary>Creates a new payment method.</summary>
public sealed record CreatePaymentMethodCommand(string Name) : IRequest<PaymentMethodDto>;

/// <summary>Handles <see cref="CreatePaymentMethodCommand"/>.</summary>
public sealed class CreatePaymentMethodCommandHandler(IPaymentMethodRepository repository) : IRequestHandler<CreatePaymentMethodCommand, PaymentMethodDto>
{
    /// <inheritdoc/>
    public async Task<PaymentMethodDto> Handle(CreatePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var method = PaymentMethod.Create(PaymentMethodName.Create(request.Name));

        await repository.AddAsync(method, cancellationToken);

        return PaymentMethodDto.FromDomain(method);
    }
}
