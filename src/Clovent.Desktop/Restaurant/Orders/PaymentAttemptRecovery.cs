using Clovent.Restaurant.Application.Payments.Commands;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>Retains an uncertain payment's immutable request until acknowledged and refreshed.</summary>
internal sealed class PaymentAttemptRecovery(IPaymentAttemptJournal? journal = null)
{
    private RecordPaymentCommand? _pending;
    private string? _pendingOwner;

    public static async Task<Exception?> RunCommittedFollowUpAsync(Func<Task> followUp)
    {
        try { await followUp(); return null; }
        catch (Exception ex) when (ex is not OutOfMemoryException) { return ex; }
    }

    public async Task ExecuteAsync(RecordPaymentCommand candidate, Func<RecordPaymentCommand, Task> commit, Func<Task> refresh)
    {
        if (candidate.Amount <= 0m)
            throw new InvalidOperationException("No payment is due. Refresh the order and complete or reprint the existing transaction; do not collect money again.");
        if (_pending is not null && journal is not null && _pendingOwner != journal.OwnerIdentity)
            throw new InvalidOperationException("Cashier changed during an unresolved payment. Reopen the screen under the original cashier and reconcile payment history.");
        if (_pending is null)
        {
            _pendingOwner = journal?.OwnerIdentity;
            _pending = journal is null
                ? candidate with { IdempotencyKey = Guid.NewGuid().ToString("N") }
                : await journal.GetOrCreateAsync(candidate);
        }
        var attempt = _pending ?? throw new InvalidOperationException("Payment attempt could not be preserved; no payment was sent.");
        if (journal is not null && _pendingOwner != journal.OwnerIdentity)
            throw new InvalidOperationException("Cashier changed before payment submission. No payment was sent; preserve the attempt for reconciliation.");
        if (attempt.OrderId != candidate.OrderId ||
            attempt.PaymentMethodId != candidate.PaymentMethodId || attempt.Amount != candidate.Amount ||
            attempt.ShiftId != candidate.ShiftId)
            throw new InvalidOperationException("An earlier payment result is unconfirmed. Refresh payment history and resolve the unchanged payment attempt before starting another payment. Do not collect money again.");

        try
        {
            // Approval is supplied by this invocation; a journal never restores it.
            await commit(attempt with { ExceedCreditLimitApproved = candidate.ExceedCreditLimitApproved });
        }
        catch (Clovent.Restaurant.RestaurantDomainException)
        {
            // RecordPayment's domain rejection occurs before persistence. It is
            // safe to start a corrected attempt after refreshing authoritative state.
            if (journal is not null) await journal.ClearAsync(attempt.OrderId, attempt.IdempotencyKey!);
            _pending = null;
            try { await refresh(); }
            catch (Exception refreshError) when (refreshError is not OutOfMemoryException) { }
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Refresh is read-only and best effort. Never replace an uncertain
            // online payment with a second offline sale or a new attempt key.
            try { await refresh(); }
            catch (Exception refreshError) when (refreshError is not OutOfMemoryException) { }
            throw new InvalidOperationException("Payment result is unconfirmed. Refresh payment history before collecting money again. Retry unchanged details using the retained attempt; do not switch this sale to offline mode.", ex);
        }

        try
        {
            await refresh();
            if (journal is not null) await journal.ClearAsync(attempt.OrderId, attempt.IdempotencyKey!);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new InvalidOperationException("Payment is saved, but its refresh/recovery checkpoint could not be completed. Do not collect money again. Refresh the order or retry unchanged details to recover the saved payment.", ex);
        }
        _pending = null;
    }
}
