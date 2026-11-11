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
/// <b>No broad AsyncLocal bypass:</b> <see cref="ExecuteWithLocksAsync{T}"/> never skips locking based on
/// inherited <see cref="AsyncLocal{T}"/> state, so parallel child tasks and escaped background continuations
/// cannot bypass mutual exclusion or extend stale ownership. Legitimate pipeline handoffs and explicit
/// sequential nested operations use single-use, reference-bound, mutable-disposal leases that enforce
/// canonical lock hierarchy ordering.
/// </para>
/// <para>
/// <b>Scope limitation:</b> This provides mutual exclusion within a single process only; multi-process
/// or multi-terminal concurrency still relies on database transactions, unique constraints, and SQL concurrency controls.
/// </para>
/// </remarks>
public static class TransactionLockManager
{
    private static readonly ConcurrentDictionary<TransactionLockKey, SemaphoreSlim> Locks = new();
    private static readonly AsyncLocal<PipelineLockLease?> ActivePipelineLease = new();

    private sealed class PipelineLockLease(
        object? boundRequest,
        ImmutableHashSet<TransactionLockKey> heldKeys,
        TransactionLockKey? maxHeldKey,
        PipelineLockLease? parent = null,
        bool allowPipelineHandoff = false) : IDisposable
    {
        private int _disposed;
        private int _claimed;
        private int _behaviorHandedOff;
        private int _nestedActive;

        public object? BoundRequest => boundRequest;
        public ImmutableHashSet<TransactionLockKey> HeldKeys => heldKeys;
        public TransactionLockKey? MaxHeldKey => maxHeldKey;
        public bool IsDisposed => Volatile.Read(ref _disposed) != 0 || (parent?.IsDisposed ?? false);

        public bool TryConsumeBehaviorHandoff(object request) =>
            allowPipelineHandoff &&
            !IsDisposed &&
            boundRequest is not null &&
            ReferenceEquals(boundRequest, request) &&
            Interlocked.Exchange(ref _behaviorHandedOff, 1) == 0;

        public bool TryClaim(object request, IEnumerable<TransactionLockKey>? requiredKeys)
        {
            if (IsDisposed || boundRequest is null || !ReferenceEquals(boundRequest, request))
            {
                return false;
            }

            if (requiredKeys is not null)
            {
                foreach (var key in requiredKeys)
                {
                    if (key.EntityId != Guid.Empty && !heldKeys.Contains(key))
                    {
                        return false;
                    }
                }
            }

            return Interlocked.Exchange(ref _claimed, 1) == 0;
        }

        public bool TryEnterNested() =>
            !IsDisposed && Interlocked.CompareExchange(ref _nestedActive, 1, 0) == 0;

        public void ExitNested() => Volatile.Write(ref _nestedActive, 0);

        public void Dispose() => Interlocked.Exchange(ref _disposed, 1);
    }

    /// <summary>Returns the canonical semaphore instance for <paramref name="key"/> (for deterministic testing/diagnostics).</summary>
    internal static SemaphoreSlim GetSemaphore(TransactionLockKey key) =>
        Locks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));

    /// <summary>
    /// Acquires all distinct <paramref name="keys"/> in canonical order for <paramref name="request"/>
    /// (or acquires only strictly higher missing keys when running inside an explicitly authorized
    /// sequential nested scope), installs a single-use lease for its matching MediatR handler,
    /// invokes <paramref name="action"/> with the keys newly acquired by this frame, and revokes
    /// the lease before releasing locks in reverse order.
    /// </summary>
    internal static async Task<T> ExecutePipelineWithLocksAsync<T>(
        object request,
        IEnumerable<TransactionLockKey> keys,
        CancellationToken cancellationToken,
        Func<IReadOnlyList<TransactionLockKey>, Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(action);

        var sortedKeys = NormalizeKeys(keys);
        var current = ActivePipelineLease.Value;

        if (current is not null && current.TryConsumeBehaviorHandoff(request))
        {
            var missingKeys = ExtractCanonicalMissingKeys(current, sortedKeys);
            var acquired = new List<SemaphoreSlim>(missingKeys.Count);
            using var childLease = new PipelineLockLease(
                request,
                current.HeldKeys.Union(missingKeys),
                missingKeys.Count > 0 ? missingKeys[^1] : current.MaxHeldKey,
                parent: current);

            try
            {
                await AcquireSortedAsync(missingKeys, acquired, cancellationToken).ConfigureAwait(false);
                ActivePipelineLease.Value = childLease;
                return await action(missingKeys).ConfigureAwait(false);
            }
            finally
            {
                childLease.Dispose();
                ActivePipelineLease.Value = current;
                ReleaseReverse(acquired);
            }
        }

        var topAcquired = new List<SemaphoreSlim>(sortedKeys.Count);
        using var lease = new PipelineLockLease(
            request,
            sortedKeys.ToImmutableHashSet(),
            sortedKeys.Count > 0 ? sortedKeys[^1] : null);

        try
        {
            await AcquireSortedAsync(sortedKeys, topAcquired, cancellationToken).ConfigureAwait(false);
            ActivePipelineLease.Value = lease;
            return await action(sortedKeys).ConfigureAwait(false);
        }
        finally
        {
            // Mutate lease disposed flag in-place before releasing locks so any forked or escaped
            // AsyncLocal context observes revocation immediately and cannot bypass synchronization.
            lease.Dispose();
            ActivePipelineLease.Value = current;
            ReleaseReverse(topAcquired);
        }
    }

    /// <summary>
    /// Executes an explicitly authorized sequential nested command under an active parent lock lease,
    /// propagating already-held locks solely for <paramref name="nestedRequest"/> while enforcing
    /// canonical lock ordering and prohibiting concurrent sibling nesting.
    /// </summary>
    public static async Task<T> ExecuteNestedRequestAsync<T>(
        object nestedRequest,
        Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(nestedRequest);
        ArgumentNullException.ThrowIfNull(action);

        var parent = ActivePipelineLease.Value;
        if (parent is null || parent.IsDisposed)
        {
            return await action().ConfigureAwait(false);
        }

        if (!parent.TryEnterNested())
        {
            throw new InvalidOperationException("Concurrent nested operations cannot share an active transaction lock lease.");
        }

        using var nestedScope = new PipelineLockLease(
            nestedRequest,
            parent.HeldKeys,
            parent.MaxHeldKey,
            parent: parent,
            allowPipelineHandoff: true);

        try
        {
            ActivePipelineLease.Value = nestedScope;
            return await action().ConfigureAwait(false);
        }
        finally
        {
            nestedScope.Dispose();
            ActivePipelineLease.Value = parent;
            parent.ExitNested();
        }
    }

    /// <summary>
    /// Runs <paramref name="action"/> immediately when an active outer MediatR pipeline lease
    /// matches the exact <paramref name="request"/> instance (single-claim).
    /// </summary>
    internal static Task<(bool HandledUnderPipelineLease, T Result)> TryRunUnderPipelineLeaseAsync<T>(
        object request,
        Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(action);

        var lease = ActivePipelineLease.Value;
        if (lease is not null && lease.TryClaim(request, requiredKeys: null))
        {
            return RunClaimedAsync(action);
        }

        return Task.FromResult<(bool, T)>((false, default!));

        static async Task<(bool, T)> RunClaimedAsync(Func<Task<T>> body) =>
            (true, await body().ConfigureAwait(false));
    }

    /// <summary>
    /// Executes <paramref name="action"/> under canonical mutual exclusion: consumes the active pipeline
    /// or authorized sequential nested lease matching <paramref name="request"/> if active, non-disposed,
    /// and canonical; otherwise acquires all <paramref name="keys"/> directly without trusting ambient
    /// inherited <see cref="AsyncLocal{T}"/> state.
    /// </summary>
    public static async Task<T> ExecuteForRequestAsync<T>(
        object request,
        IEnumerable<TransactionLockKey> keys,
        CancellationToken cancellationToken,
        Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(action);

        var lease = ActivePipelineLease.Value;
        if (lease is not null)
        {
            if (lease.TryConsumeBehaviorHandoff(request))
            {
                var sortedKeys = NormalizeKeys(keys);
                var missingKeys = ExtractCanonicalMissingKeys(lease, sortedKeys);
                var acquired = new List<SemaphoreSlim>(missingKeys.Count);
                using var childLease = new PipelineLockLease(
                    request,
                    lease.HeldKeys.Union(missingKeys),
                    missingKeys.Count > 0 ? missingKeys[^1] : lease.MaxHeldKey,
                    parent: lease);

                try
                {
                    await AcquireSortedAsync(missingKeys, acquired, cancellationToken).ConfigureAwait(false);
                    ActivePipelineLease.Value = childLease;
                    return await action().ConfigureAwait(false);
                }
                finally
                {
                    childLease.Dispose();
                    ActivePipelineLease.Value = lease;
                    ReleaseReverse(acquired);
                }
            }

            if (lease.TryClaim(request, keys))
            {
                return await action().ConfigureAwait(false);
            }
        }

        return await ExecuteWithLocksAsync(keys, cancellationToken, action).ConfigureAwait(false);
    }

    /// <summary>
    /// Strictly acquires all distinct <paramref name="keys"/> in canonical <see cref="TransactionLockKey"/>
    /// order, executes <paramref name="action"/>, and releases all acquired locks in reverse order.
    /// Never bypasses locking via inherited <see cref="AsyncLocal{T}"/> state.
    /// </summary>
    public static async Task<T> ExecuteWithLocksAsync<T>(
        IEnumerable<TransactionLockKey> keys,
        CancellationToken cancellationToken,
        Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(action);

        var sortedKeys = NormalizeKeys(keys);
        var previous = ActivePipelineLease.Value;
        if (sortedKeys.Count == 0)
        {
            return await action().ConfigureAwait(false);
        }

        var acquired = new List<SemaphoreSlim>(sortedKeys.Count);
        using var scopeLease = new PipelineLockLease(
            boundRequest: null,
            sortedKeys.ToImmutableHashSet(),
            sortedKeys[^1]);

        try
        {
            await AcquireSortedAsync(sortedKeys, acquired, cancellationToken).ConfigureAwait(false);
            ActivePipelineLease.Value = scopeLease;
            return await action().ConfigureAwait(false);
        }
        finally
        {
            scopeLease.Dispose();
            ActivePipelineLease.Value = previous;
            ReleaseReverse(acquired);
        }
    }

    private static List<TransactionLockKey> NormalizeKeys(IEnumerable<TransactionLockKey> keys) =>
        keys.Where(k => k.EntityId != Guid.Empty)
            .Distinct()
            .OrderBy(k => k)
            .ToList();

    private static List<TransactionLockKey> ExtractCanonicalMissingKeys(
        PipelineLockLease current,
        IReadOnlyList<TransactionLockKey> sortedRequestedKeys)
    {
        var missing = new List<TransactionLockKey>();
        foreach (var key in sortedRequestedKeys)
        {
            if (!current.HeldKeys.Contains(key))
            {
                missing.Add(key);
            }
        }

        if (missing.Count > 0 && current.MaxHeldKey is { } maxHeld && missing[0].CompareTo(maxHeld) <= 0)
        {
            throw new InvalidOperationException(
                $"Canonical transaction lock hierarchy violation: cannot acquire {missing[0]} while already holding {maxHeld}.");
        }

        return missing;
    }

    private static async Task AcquireSortedAsync(
        IReadOnlyList<TransactionLockKey> sortedKeys,
        List<SemaphoreSlim> acquired,
        CancellationToken cancellationToken)
    {
        foreach (var key in sortedKeys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var semaphore = GetSemaphore(key);
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            acquired.Add(semaphore);
        }
    }

    private static void ReleaseReverse(List<SemaphoreSlim> acquired)
    {
        for (var i = acquired.Count - 1; i >= 0; i--)
        {
            acquired[i].Release();
        }
    }
}
