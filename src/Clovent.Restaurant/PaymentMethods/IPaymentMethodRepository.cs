namespace Clovent.Restaurant.PaymentMethods;

/// <summary>Persistence contract for <see cref="PaymentMethod"/> aggregates.</summary>
public interface IPaymentMethodRepository
{
    /// <summary>Retrieves a payment method by identity, or <see langword="null"/> if none exists.</summary>
    Task<PaymentMethod?> GetByIdAsync(PaymentMethodId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every payment method.</summary>
    Task<IReadOnlyCollection<PaymentMethod>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created payment method.</summary>
    Task AddAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default);
}
