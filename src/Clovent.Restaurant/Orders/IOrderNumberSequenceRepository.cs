namespace Clovent.Restaurant.Orders;

/// <summary>Persistence contract for the single, global <see cref="OrderNumberSequence"/> row.</summary>
public interface IOrderNumberSequenceRepository
{
    /// <summary>Retrieves the one sequence row, or <see langword="null"/> if it has never been created yet.</summary>
    Task<OrderNumberSequence?> GetSingletonAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds the sequence row on first use.</summary>
    Task AddAsync(OrderNumberSequence sequence, CancellationToken cancellationToken = default);
}
