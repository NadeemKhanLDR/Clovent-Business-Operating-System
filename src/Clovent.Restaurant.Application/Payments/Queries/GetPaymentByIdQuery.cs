using Clovent.Restaurant.Application.Payments.Dtos;
using Clovent.Restaurant.Payments;
using MediatR;

namespace Clovent.Restaurant.Application.Payments.Queries;

/// <summary>Retrieves a payment by id.</summary>
public sealed record GetPaymentByIdQuery(Guid PaymentId) : IRequest<PaymentDto>;

/// <summary>Handles <see cref="GetPaymentByIdQuery"/>.</summary>
public sealed class GetPaymentByIdQueryHandler(IPaymentRepository repository) : IRequestHandler<GetPaymentByIdQuery, PaymentDto>
{
    /// <inheritdoc/>
    public async Task<PaymentDto> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        var payment = await repository.GetByIdAsync(new PaymentId(request.PaymentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), request.PaymentId);

        return PaymentDto.FromDomain(payment);
    }
}
