using Clovent.Restaurant.Application.Outbox.Dtos;

namespace Clovent.Restaurant.Application.QuickBooks;

/// <summary>Result of synchronizing a transaction to QuickBooks.</summary>
public sealed record QuickBooksSyncResult(
    bool Success,
    string? ExternalTransactionId,
    string? ErrorMessage);

/// <summary>Gateway abstraction for synchronizing sales with QuickBooks Online / Desktop.</summary>
public interface IQuickBooksGateway
{
    /// <summary>Posts a completed order to QuickBooks.</summary>
    Task<QuickBooksSyncResult> SyncOrderSaleAsync(QuickBooksSyncPayload payload, CancellationToken cancellationToken = default);
}

/// <summary>Standard QuickBooks gateway implementing idempotent invoice creation and fault simulation.</summary>
public sealed class DefaultQuickBooksGateway : IQuickBooksGateway
{
    // Thread-safe dictionary tracking exported external sales
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, string> _syncedOrders = new();
    private volatile bool _isSimulatingOutage;

    /// <summary>Simulates a QuickBooks service outage for testing / circuit breaker verification.</summary>
    public void SetSimulatedOutage(bool isOutage) => _isSimulatingOutage = isOutage;

    /// <inheritdoc/>
    public async Task<QuickBooksSyncResult> SyncOrderSaleAsync(QuickBooksSyncPayload payload, CancellationToken cancellationToken = default)
    {
        if (_isSimulatingOutage)
        {
            throw new HttpRequestException("QuickBooks API connection timeout: remote server 503 Service Unavailable.");
        }

        // Small simulated network latency
        await Task.Delay(10, cancellationToken).ConfigureAwait(false);

        var qbRef = _syncedOrders.GetOrAdd(payload.OrderId, id => $"QB-INV-{payload.OrderNumber}-{id.ToString()[..8].ToUpperInvariant()}");
        return new QuickBooksSyncResult(true, qbRef, null);
    }
}
