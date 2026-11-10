using Clovent.Restaurant.Application.Payments.Commands;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>Retains an uncertain payment's immutable request until acknowledged and refreshed.</summary>
internal sealed class PaymentAttemptRecovery
{
    private RecordPaymentCommand? _pending;

    public static async Task<Exception?> RunCommittedFollowUpAsync(Func<Task> followUp)
    {
        try { await followUp(); return null; }
        catch (Exception ex) when (ex is not OutOfMemoryException) { return ex; }
    }

    public async Task ExecuteAsync(RecordPaymentCommand candidate, Func<RecordPaymentCommand, Task> commit, Func<Task> refresh)
    {
        if (_pending is not null && (_pending.OrderId != candidate.OrderId ||
            _pending.PaymentMethodId != candidate.PaymentMethodId || _pending.Amount != candidate.Amount ||
            _pending.ShiftId != candidate.ShiftId))
            throw new InvalidOperationException("An earlier payment result is unconfirmed. Refresh payment history and resolve the unchanged payment attempt before starting another payment. Do not collect money again.");

        _pending ??= candidate with { IdempotencyKey = Guid.NewGuid().ToString("N") };
        try
        {
            await commit(_pending);
        }
        catch (Clovent.Restaurant.RestaurantDomainException)
        {
            // RecordPayment's domain rejection occurs before persistence. It is
            // safe to start a corrected attempt after refreshing authoritative state.
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
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new InvalidOperationException("Payment is saved, but the order could not be refreshed. Do not collect money again. Refresh the order or retry unchanged details to recover the saved payment.", ex);
        }
        _pending = null;
    }
}
