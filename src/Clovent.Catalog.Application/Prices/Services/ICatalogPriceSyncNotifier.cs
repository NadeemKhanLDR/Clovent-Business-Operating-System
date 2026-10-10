using Clovent.Catalog.Prices;

namespace Clovent.Catalog.Application.Prices.Services;

/// <summary>
/// Domain service interface notifying cross-terminal replication systems of catalog price updates
/// without introducing architectural dependencies on other bounded contexts.
/// </summary>
public interface ICatalogPriceSyncNotifier
{
    /// <summary>Notifies replication listeners that a catalog price amount has been updated.</summary>
    Task NotifyPriceUpdatedAsync(ProductPrice price, decimal oldAmount, CancellationToken cancellationToken = default);
}
