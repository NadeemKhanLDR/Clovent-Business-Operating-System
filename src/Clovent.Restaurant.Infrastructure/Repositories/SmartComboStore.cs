using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.SmartCombos;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.SmartCombos;
using Microsoft.EntityFrameworkCore;
namespace Clovent.Restaurant.Infrastructure.Repositories;

public sealed class SmartComboStore(RestaurantDbContext db) : ISmartComboStore
{
    public async Task<IReadOnlyList<ComboBasket>> ReadBasketsAsync(Guid warehouseId, DateTimeOffset from, DateTimeOffset to, int maximumOrders, CancellationToken ct, bool includeDevelopmentSamples = false)
    {
        var ordersQuery = db.Orders.AsNoTracking().Where(o => o.WarehouseId == new WarehouseId(warehouseId) &&
            (includeDevelopmentSamples || o.Notes == null || !o.Notes.StartsWith("DEV-SMARTCOMBO:")) &&
            o.Status == OrderStatus.Completed && o.UpdatedAtUtc >= from && o.UpdatedAtUtc < to &&
            (o.Notes == null || (!o.Notes.StartsWith("QA") && !o.Notes.StartsWith("TEST") && !o.Notes.StartsWith("[QA]"))));
        var orders = await ordersQuery.OrderBy(o => o.Id).Select(o => new { o.Id, o.OrderLineIds }).Take(maximumOrders + 1).ToListAsync(ct);
        if (orders.Count > maximumOrders) throw new InvalidOperationException("Analysis volume limit exceeded. Select a shorter period.");
        var lines = await (from line in db.OrderLines.AsNoTracking()
            join order in ordersQuery on line.OrderId equals order.Id
            where !line.IsVoided && line.Quantity > 0 && line.UnitPrice > 0
            select new { line.Id, line.OrderId, line.ProductVariantId }).ToListAsync(ct);
        var byOrder = lines.ToLookup(x => x.OrderId);
        return orders.Select(o => new ComboBasket(o.Id.Value, byOrder[o.Id].Where(l => o.OrderLineIds.Contains(l.Id))
            .Select(l => l.ProductVariantId.Value).Distinct().ToArray())).Where(x => x.Variants.Count > 0).ToList();
    }
    public async Task<IReadOnlyList<ComboDecision>> DecisionsAsync(Guid warehouseId, CancellationToken ct) =>
        await db.Set<ComboDecision>().AsNoTracking().Where(x => x.WarehouseId == warehouseId).ToListAsync(ct);
    public async Task<ComboDecision> DecisionAsync(Guid warehouseId, string signature, CancellationToken ct)
    {
        var decision = await db.Set<ComboDecision>().FindAsync([warehouseId, signature], ct);
        if (decision != null) return decision;
        decision = ComboDecision.Create(warehouseId, signature);
        db.Add(decision);
        return decision;
    }
}



