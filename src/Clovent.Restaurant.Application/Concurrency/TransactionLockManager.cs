using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Clovent.Restaurant.Application.Concurrency;

/// <summary>
/// Canonical lock hierarchy ordering for in-process and SQL session transaction serialization.
/// Numerical order defines the strict global acquisition order
/// (<see cref="BusinessDay"/> &lt; <see cref="Shift"/> &lt; <see cref="Customer"/> &lt; <see cref="Table"/> &lt; <see cref="Order"/> &lt; <see cref="Continuity"/>)
/// to make multi-resource lock deadlocks impossible.
/// </summary>
public enum TransactionLockScope
{
    /// <summary>Branch business-day lifecycle operations (Exclusive for day close / shift open; Shared for financial postings).</summary>
    BusinessDay = 1,

    /// <summary>Shift-level operations (close shift, cash movement, shift-attributed payments/refunds).</summary>
    Shift = 2,

    /// <summary>Customer-level shared state (OutstandingBalance, credit limit, advances, allocations).</summary>
    Customer = 3,

    /// <summary>Table-level occupancy and bill ownership operations (create order, transfer table, split order, merge tables, vacate/reopen).</summary>
    Table = 4,

    /// <summary>Order-level operations (order/line/discount mutations, payments, payment voids, completion, order voids, refunds).</summary>
    Order = 5,

    /// <summary>Offline Continuity journal transaction replay and emergency idempotency coordination.</summary>
    Continuity = 6
}

/// <summary>Conflict mode for a lock acquisition on a resource.</summary>
public enum TransactionLockAccessMode
{
    /// <summary>Exclusive writer lock (mutually exclusive with all other Exclusive and Shared holders).</summary>
    Exclusive = 0,

    /// <summary>Shared reader/intent lock (compatible with other Shared holders; blocked by Exclusive writers).</summary>
    Shared = 1
}

/// <summary>Comparable lock key uniquely identifying a locked resource and access mode in global acquisition order.</summary>
public readonly record struct TransactionLockKey(
    TransactionLockScope Scope,
    Guid EntityId,
    TransactionLockAccessMode AccessMode = TransactionLockAccessMode.Exclusive)
    : IComparable<TransactionLockKey>
{
    /// <summary>Identifies the underlying entity resource regardless of <see cref="AccessMode"/>.</summary>
    public (TransactionLockScope Scope, Guid EntityId) Resource => (Scope, EntityId);

    /// <inheritdoc/>
    public int CompareTo(TransactionLockKey other)
    {
        var scopeCmp = ((int)Scope).CompareTo((int)other.Scope);
        if (scopeCmp != 0) return scopeCmp;

        var idCmp = EntityId.CompareTo(other.EntityId);
        return idCmp != 0 ? idCmp : ((int)AccessMode).CompareTo((int)other.AccessMode);
    }

    /// <summary>Creates a branch business-day lock key.</summary>
    public static TransactionLockKey ForBusinessDay(
        Guid branchId,
        TransactionLockAccessMode accessMode = TransactionLockAccessMode.Exclusive) =>
        new(TransactionLockScope.BusinessDay, branchId, accessMode);

    /// <summary>Creates a shift lock key.</summary>
    public static TransactionLockKey ForShift(Guid shiftId) => new(TransactionLockScope.Shift, shiftId);

    /// <summary>Creates a customer lock key.</summary>
    public static TransactionLockKey ForCustomer(Guid customerId) => new(TransactionLockScope.Customer, customerId);

    /// <summary>Creates a table lock key.</summary>
    public static TransactionLockKey ForTable(Guid tableId) => new(TransactionLockScope.Table, tableId);

    /// <summary>Creates an order lock key.</summary>
    public static TransactionLockKey ForOrder(Guid orderId) => new(TransactionLockScope.Order, orderId);

    /// <summary>Creates a continuity transaction replay lock key.</summary>
    public static TransactionLockKey ForContinuityTransaction(Guid transactionId) => new(TransactionLockScope.Continuity, transactionId);
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
    private static readonly ConcurrentDictionary<(TransactionLockScope Scope, Guid EntityId), AsyncLockGate> Locks = new();
    private static readonly AsyncLocal<PipelineLockLease?> ActivePipelineLease = new();

    private sealed class AsyncLockGate
    {
        private readonly object _sync = new();
        private readonly Queue<Waiter> _waiters = new();
        private int _state; // 0 = unlocked, >0 = active shared readers, -1 = active exclusive writer

        /// <summary>Permanent non-evicting identity marker for diagnostics/tests.</summary>
        public SemaphoreSlim IdentityMarker { get; } = new(1, 1);

        public Task<LockHold> AcquireAsync(TransactionLockAccessMode mode, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<LockHold>(cancellationToken);
            }

            lock (_sync)
            {
                if (mode == TransactionLockAccessMode.Shared)
                {
                    if (_state >= 0 && _waiters.Count == 0)
                    {
                        _state++;
                        return Task.FromResult(new LockHold(this, mode));
                    }
                }
                else
                {
                    if (_state == 0 && _waiters.Count == 0)
                    {
                        _state = -1;
                        return Task.FromResult(new LockHold(this, mode));
                    }
                }

                var waiter = new Waiter(this, mode);
                _waiters.Enqueue(waiter);
                waiter.AttachCancellation(cancellationToken);
                return waiter.Tcs.Task;
            }
        }

        public void Release(TransactionLockAccessMode mode)
        {
            lock (_sync)
            {
                if (mode == TransactionLockAccessMode.Exclusive)
                {
                    _state = 0;
                }
                else
                {
                    _state--;
                }

                PumpWaitersLocked();
            }
        }

        private void OnWaiterCancelled(Waiter waiter, CancellationToken token)
        {
            var shouldCancel = false;
            lock (_sync)
            {
                if (waiter.TryClaim(isCancel: true))
                {
                    shouldCancel = true;
                    PumpWaitersLocked();
                }
            }

            if (shouldCancel)
            {
                waiter.Tcs.TrySetCanceled(token);
            }
        }

        private void PumpWaitersLocked()
        {
            while (_waiters.Count > 0)
            {
                var head = _waiters.Peek();
                if (head.IsFinished)
                {
                    _waiters.Dequeue();
                    continue;
                }

                if (head.Mode == TransactionLockAccessMode.Exclusive)
                {
                    if (_state == 0 && head.TryClaim(isCancel: false))
                    {
                        _waiters.Dequeue();
                        _state = -1;
                        head.CompleteGranted(new LockHold(this, head.Mode));
                    }
                    break;
                }

                if (_state >= 0)
                {
                    _waiters.Dequeue();
                    if (head.TryClaim(isCancel: false))
                    {
                        _state++;
                        head.CompleteGranted(new LockHold(this, head.Mode));
                    }
                    continue;
                }

                break;
            }
        }

        private sealed class Waiter(AsyncLockGate owner, TransactionLockAccessMode mode)
        {
            private int _done; // 0 = pending, 1 = granted, 2 = cancelled
            private CancellationTokenRegistration _registration;

            public TransactionLockAccessMode Mode => mode;
            public TaskCompletionSource<LockHold> Tcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public bool IsFinished => Volatile.Read(ref _done) != 0;

            public void AttachCancellation(CancellationToken ct)
            {
                if (ct.CanBeCanceled)
                {
                    _registration = ct.Register(static state =>
                    {
                        var (w, token) = ((Waiter, CancellationToken))state!;
                        w._owner.OnWaiterCancelled(w, token);
                    }, (this, ct));
                }
            }

            private readonly AsyncLockGate _owner = owner;

            public bool TryClaim(bool isCancel) =>
                Interlocked.CompareExchange(ref _done, isCancel ? 2 : 1, 0) == 0;

            public void CompleteGranted(LockHold hold)
            {
                _registration.Unregister();
                Tcs.TrySetResult(hold);
            }
        }
    }

    private readonly record struct LockHold(AsyncLockGate Gate, TransactionLockAccessMode Mode);

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

        public bool IsKeySatisfied(TransactionLockKey required)
        {
            foreach (var held in heldKeys)
            {
                if (held.Scope == required.Scope && held.EntityId == required.EntityId)
                {
                    return held.AccessMode == TransactionLockAccessMode.Exclusive ||
                           required.AccessMode == TransactionLockAccessMode.Shared;
                }
            }

            return false;
        }

        public bool HasResourceConflictUpgrade(TransactionLockKey required)
        {
            if (required.AccessMode != TransactionLockAccessMode.Exclusive)
            {
                return false;
            }

            foreach (var held in heldKeys)
            {
                if (held.Scope == required.Scope &&
                    held.EntityId == required.EntityId &&
                    held.AccessMode == TransactionLockAccessMode.Shared)
                {
                    return true;
                }
            }

            return false;
        }

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
                    if (key.EntityId != Guid.Empty && !IsKeySatisfied(key))
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

    /// <summary>Returns the canonical non-evicting lock marker for <paramref name="key"/> (for deterministic testing/diagnostics).</summary>
    internal static SemaphoreSlim GetSemaphore(TransactionLockKey key) =>
        GetGate(key.Resource).IdentityMarker;

    private static AsyncLockGate GetGate((TransactionLockScope Scope, Guid EntityId) resource) =>
        Locks.GetOrAdd(resource, static _ => new AsyncLockGate());

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
            var acquired = new List<LockHold>(missingKeys.Count);
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

        var topAcquired = new List<LockHold>(sortedKeys.Count);
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
                var acquired = new List<LockHold>(missingKeys.Count);
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

        var acquired = new List<LockHold>(sortedKeys.Count);
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
            .GroupBy(k => k.Resource)
            .Select(g => new TransactionLockKey(
                g.Key.Scope,
                g.Key.EntityId,
                g.Any(k => k.AccessMode == TransactionLockAccessMode.Exclusive)
                    ? TransactionLockAccessMode.Exclusive
                    : TransactionLockAccessMode.Shared))
            .OrderBy(k => k)
            .ToList();

    private static List<TransactionLockKey> ExtractCanonicalMissingKeys(
        PipelineLockLease current,
        IReadOnlyList<TransactionLockKey> sortedRequestedKeys)
    {
        var missing = new List<TransactionLockKey>();
        foreach (var key in sortedRequestedKeys)
        {
            if (current.HasResourceConflictUpgrade(key))
            {
                throw new InvalidOperationException(
                    $"Canonical transaction lock hierarchy violation: cannot upgrade held Shared lock on {key.Scope}:{key.EntityId} to Exclusive.");
            }

            if (!current.IsKeySatisfied(key))
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
        List<LockHold> acquired,
        CancellationToken cancellationToken)
    {
        foreach (var key in sortedKeys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var gate = GetGate(key.Resource);
            var hold = await gate.AcquireAsync(key.AccessMode, cancellationToken).ConfigureAwait(false);
            acquired.Add(hold);
        }
    }

    private static void ReleaseReverse(List<LockHold> acquired)
    {
        for (var i = acquired.Count - 1; i >= 0; i--)
        {
            acquired[i].Gate.Release(acquired[i].Mode);
        }
    }
}
