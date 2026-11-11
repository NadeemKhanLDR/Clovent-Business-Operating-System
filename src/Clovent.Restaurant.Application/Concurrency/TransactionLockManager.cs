using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Clovent.Restaurant.Application.Concurrency;

/// <summary>
/// Canonical lock hierarchy ordering for in-process transaction serialization.
/// Numerical order defines the strict global acquisition order
/// (<see cref="Shift"/> &lt; <see cref="Customer"/> &lt; <see cref="Order"/>)
/// to make multi-resource lock deadlocks impossible.
/// </summary>
public enum TransactionLockScope
{
    /// <summary>Shift-level operations (close shift, cash movement, shift-attributed payments/refunds).</summary>
    Shift = 1,

    /// <summary>Customer-level shared state (OutstandingBalance, credit limit, advances, allocations).</summary>
    Customer = 2,

    /// <summary>Order-level operations (payments, payment voids, completion, order voids, refunds).</summary>
    Order = 3
}

/// <summary>Comparable lock key uniquely identifying a locked aggregate in global acquisition order.</summary>
public readonly record struct TransactionLockKey(TransactionLockScope Scope, Guid EntityId)
    : IComparable<TransactionLockKey>
{
    /// <inheritdoc/>
    public int CompareTo(TransactionLockKey other)
    {
        var scopeCmp = ((int)Scope).CompareTo((int)other.Scope);
        return scopeCmp != 0 ? scopeCmp : EntityId.CompareTo(other.EntityId);
    }

    /// <summary>Creates a shift lock key.</summary>
    public static TransactionLockKey ForShift(Guid shiftId) => new(TransactionLockScope.Shift, shiftId);

    /// <summary>Creates a customer lock key.</summary>
    public static TransactionLockKey ForCustomer(Guid customerId) => new(TransactionLockScope.Customer, customerId);

    /// <summary>Creates an order lock key.</summary>
    public static TransactionLockKey ForOrder(Guid orderId) => new(TransactionLockScope.Order, orderId);
}

/// <summary>
/// Deadlock-free, non-evicting in-process lock manager serializing financial commands across
/// shifts, customers, and orders.
/// </summary>
/// <remarks>
/// <para>
/// <b>Non-eviction guarantee:</b> <see cref="SemaphoreSlim"/> instances are stored in a permanent
/// <see cref="ConcurrentDictionary{TKey,TValue}"/> and are never evicted. Evicting semaphores would
/// allow two callers for the same entity key to observe different semaphore objects and bypass
/// mutual exclusion.
/// </para>
/// <para>
/// <b>Scope limitation:</b> This provides mutual exclusion within a single process only; multi-process
/// or multi-terminal concurrency still relies on database transactions, unique constraints, and SQL concurrency controls.
/// </para>
/// </remarks>
public static class TransactionLockManager
{
    private static readonly ConcurrentDictionary<TransactionLockKey, SemaphoreSlim> Locks = new();
    private static readonly AsyncLocal<ImmutableHashSet<TransactionLockKey>?> HeldKeys = new();

    /// <summary>Returns the canonical semaphore instance for <paramref name="key"/> (for deterministic testing/diagnostics).</summary>
    internal static SemaphoreSlim GetSemaphore(TransactionLockKey key) =>
        Locks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));

    /// <summary>
    /// Acquires all distinct <paramref name="keys"/> (not already held on the current async flow)
    /// in strict canonical <see cref="TransactionLockKey"/> order, executes <paramref name="action"/>,
    /// and releases all newly acquired locks in reverse order in a <c>finally</c> block.
    /// </summary>
    public static async Task<T> ExecuteWithLocksAsync<T>(
        IEnumerable<TransactionLockKey> keys,
        CancellationToken cancellationToken,
        Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(action);

        var previousHeld = HeldKeys.Value ?? ImmutableHashSet<TransactionLockKey>.Empty;
        var toAcquire = keys
            .Where(k => k.EntityId != Guid.Empty && !previousHeld.Contains(k))
            .Distinct()
            .OrderBy(k => k)
            .ToList();

        if (toAcquire.Count == 0)
        {
            return await action().ConfigureAwait(false);
        }

        var acquired = new List<SemaphoreSlim>(toAcquire.Count);
        try
        {
            foreach (var key in toAcquire)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var semaphore = GetSemaphore(key);
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                acquired.Add(semaphore);
            }

            HeldKeys.Value = previousHeld.Union(toAcquire);
            return await action().ConfigureAwait(false);
        }
        finally
        {
            HeldKeys.Value = previousHeld;
            for (var i = acquired.Count - 1; i >= 0; i--)
            {
                acquired[i].Release();
            }
        }
    }
}
