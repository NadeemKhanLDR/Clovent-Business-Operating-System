using System.Collections.Concurrent;

namespace Clovent.Restaurant.Application.Orders;

/// <summary>
/// Shared per-order synchronization locks serializing concurrent payment recording,
/// payment voiding, and order voiding operations targeting the same order.
/// </summary>
internal static class OrderTransactionLocks
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> Locks = new();

    public static SemaphoreSlim GetLock(Guid orderId) =>
        Locks.GetOrAdd(orderId, static _ => new SemaphoreSlim(1, 1));
}
