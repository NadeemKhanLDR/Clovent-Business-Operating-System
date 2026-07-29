using Clovent.Restaurant.Application.PaymentMethods.Dtos;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using MediatR;

namespace Clovent.Restaurant.Application.PaymentMethods.Commands;

/// <summary>Renames a payment method.</summary>
public sealed record RenamePaymentMethodCommand(Guid PaymentMethodId, string Name) : IRequest<PaymentMethodDto>;

/// <summary>Handles <see cref="RenamePaymentMethodCommand"/>.</summary>
public sealed class RenamePaymentMethodCommandHandler(IPaymentMethodRepository repository) : IRequestHandler<RenamePaymentMethodCommand, PaymentMethodDto>
{
    /// <inheritdoc/>
    public async Task<PaymentMethodDto> Handle(RenamePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var method = await repository.GetByIdAsync(new PaymentMethodId(request.PaymentMethodId), cancellationToken)
            ?? throw new NotFoundException(nameof(PaymentMethod), request.PaymentMethodId);

        method.Rename(PaymentMethodName.Create(request.Name));
        return PaymentMethodDto.FromDomain(method);
    }
}
