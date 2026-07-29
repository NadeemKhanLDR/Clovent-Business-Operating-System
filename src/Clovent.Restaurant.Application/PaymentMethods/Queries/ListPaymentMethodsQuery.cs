using Clovent.Restaurant.Application.PaymentMethods.Dtos;
using Clovent.Restaurant.PaymentMethods;
using MediatR;

namespace Clovent.Restaurant.Application.PaymentMethods.Queries;

/// <summary>Retrieves every payment method.</summary>
public sealed record ListPaymentMethodsQuery : IRequest<IReadOnlyCollection<PaymentMethodDto>>;

/// <summary>Handles <see cref="ListPaymentMethodsQuery"/>.</summary>
public sealed class ListPaymentMethodsQueryHandler(IPaymentMethodRepository repository)
    : IRequestHandler<ListPaymentMethodsQuery, IReadOnlyCollection<PaymentMethodDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<PaymentMethodDto>> Handle(ListPaymentMethodsQuery request, CancellationToken cancellationToken)
    {
        var methods = await repository.GetAllAsync(cancellationToken);
        return [.. methods.Select(PaymentMethodDto.FromDomain)];
    }
}
