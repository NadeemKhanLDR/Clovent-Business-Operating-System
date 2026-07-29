using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.Payments;

/// <summary>Persistence contract for <see cref="Payment"/> aggregates.</summary>
public interface IPaymentRepository
{
    /// <summary>Retrieves a payment by identity, or <see langword="null"/> if none exists.</summary>
    Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every payment recorded against an order.</summary>
    Task<IReadOnlyCollection<Payment>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-recorded payment.</summary>
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
}
