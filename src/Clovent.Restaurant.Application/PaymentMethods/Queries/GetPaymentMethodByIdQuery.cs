using Clovent.Restaurant.Application.PaymentMethods.Dtos;
using Clovent.Restaurant.PaymentMethods;
using MediatR;

namespace Clovent.Restaurant.Application.PaymentMethods.Queries;

/// <summary>Retrieves a payment method by id.</summary>
public sealed record GetPaymentMethodByIdQuery(Guid PaymentMethodId) : IRequest<PaymentMethodDto>;

/// <summary>Handles <see cref="GetPaymentMethodByIdQuery"/>.</summary>
public sealed class GetPaymentMethodByIdQueryHandler(IPaymentMethodRepository repository) : IRequestHandler<GetPaymentMethodByIdQuery, PaymentMethodDto>
{
    /// <inheritdoc/>
    public async Task<PaymentMethodDto> Handle(GetPaymentMethodByIdQuery request, CancellationToken cancellationToken)
    {
        var method = await repository.GetByIdAsync(new PaymentMethodId(request.PaymentMethodId), cancellationToken)
            ?? throw new NotFoundException(nameof(PaymentMethod), request.PaymentMethodId);

        return PaymentMethodDto.FromDomain(method);
    }
}
