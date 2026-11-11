using System.Text.Json;
using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.Application.Reports;

/// <summary>Report attribution for frozen sales, independent of later delivery updates.</summary>
internal static class CompletedSaleDate
{
    internal static DateTimeOffset For(Order order)
    {
        if (!string.IsNullOrWhiteSpace(order.ReceiptSnapshotJson))
        {
            try
            {
                var snapshot = JsonSerializer.Deserialize<ReceiptSnapshot>(order.ReceiptSnapshotJson);
                if (snapshot is not null && snapshot.OrderId == order.Id.Value && snapshot.CompletedAtUtc != default)
                    return snapshot.CompletedAtUtc;
            }
            catch (JsonException)
            {
                // Legacy unusable snapshots retain the pre-existing timestamp fallback.
            }
        }
        return order.UpdatedAtUtc;
    }
}
