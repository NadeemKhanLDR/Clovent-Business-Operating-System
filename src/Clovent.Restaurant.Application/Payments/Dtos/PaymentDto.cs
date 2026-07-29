using Clovent.Restaurant.Payments;

namespace Clovent.Restaurant.Application.Payments.Dtos;

/// <summary>Read-model shape for a <see cref="Payment"/>, safe to cross a process boundary.</summary>
public sealed record PaymentDto(Guid PaymentId, Guid OrderId, Guid PaymentMethodId, decimal Amount, bool IsVoided, DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="Payment"/> into its DTO.</summary>
    public static PaymentDto FromDomain(Payment payment) => new(
        payment.Id.Value, payment.OrderId.Value, payment.PaymentMethodId.Value, payment.Amount, payment.IsVoided, payment.CreatedAtUtc);
}
