using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Clovent.Restaurant.KitchenTickets;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.ServiceCharges;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Persistence;

public sealed class ValueComparerContractTests : SqliteTestBase
{
    [Fact]
    public void WiredHashSetComparers_EquivalentSetsInsertedInDifferentOrders_ProduceEqualHashCodes()
    {
        using var db = CreateContext();

        AssertComparerOrderInvariance<Order, OrderLineId>(
            db,
            nameof(Order.OrderLineIds),
            new OrderLineId(Guid.Parse("00000000-0000-0000-0000-000000000002")),
            new OrderLineId(Guid.Parse("00000000-0000-0000-0000-000000000001")));

        AssertComparerOrderInvariance<Order, DiscountId>(
            db,
            nameof(Order.DiscountIds),
            new DiscountId(Guid.Parse("00000000-0000-0000-0000-000000000002")),
            new DiscountId(Guid.Parse("00000000-0000-0000-0000-000000000001")));

        AssertComparerOrderInvariance<Order, ServiceChargeId>(
            db,
            nameof(Order.ServiceChargeIds),
            new ServiceChargeId(Guid.Parse("00000000-0000-0000-0000-000000000002")),
            new ServiceChargeId(Guid.Parse("00000000-0000-0000-0000-000000000001")));

        AssertComparerOrderInvariance<Order, PaymentId>(
            db,
            nameof(Order.PaymentIds),
            new PaymentId(Guid.Parse("00000000-0000-0000-0000-000000000002")),
            new PaymentId(Guid.Parse("00000000-0000-0000-0000-000000000001")));

        AssertComparerOrderInvariance<KitchenTicket, OrderLineId>(
            db,
            nameof(KitchenTicket.OrderLineIds),
            new OrderLineId(Guid.Parse("00000000-0000-0000-0000-000000000002")),
            new OrderLineId(Guid.Parse("00000000-0000-0000-0000-000000000001")));
    }

    [Fact]
    public async Task HashSetConverters_RoundTripReloadAndInPlaceMutation_PreservesPersistedValuesAndChangeTracking()
    {
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        var line1 = OrderLineId.New();
        var line2 = OrderLineId.New();
        var disc1 = DiscountId.New();
        var svc1 = ServiceChargeId.New();
        var pay1 = PaymentId.New();
        var pay2 = PaymentId.New();
        order.AddOrderLine(line1);
        order.ApplyDiscount(disc1);
        order.ApplyServiceCharge(svc1);
        order.RecordPayment(pay1);

        var ticket = KitchenTicket.Create(order.Id, [line1]);

        await using (var seedDb = CreateContext())
        {
            await seedDb.Orders.AddAsync(order);
            await seedDb.KitchenTickets.AddAsync(ticket);
            await seedDb.SaveChangesAsync();
        }

        await using (var editDb = CreateContext())
        {
            var loaded = await editDb.Orders.SingleAsync(o => o.Id == order.Id);
            Assert.False(editDb.ChangeTracker.HasChanges());

            await editDb.Entry(loaded).ReloadAsync();
            Assert.False(editDb.ChangeTracker.HasChanges());

            loaded.AddOrderLine(line2);
            loaded.RecordPayment(pay2);
            editDb.ChangeTracker.DetectChanges();
            Assert.True(editDb.ChangeTracker.HasChanges());

            await editDb.SaveChangesAsync();
        }

        await using var verifyDb = CreateContext();
        var persistedOrder = await verifyDb.Orders.AsNoTracking().SingleAsync(o => o.Id == order.Id);
        var persistedTicket = await verifyDb.KitchenTickets.AsNoTracking().SingleAsync(t => t.Id == ticket.Id);

        Assert.Equal(2, persistedOrder.OrderLineIds.Count);
        Assert.Contains(line1, persistedOrder.OrderLineIds);
        Assert.Contains(line2, persistedOrder.OrderLineIds);
        Assert.Single(persistedOrder.DiscountIds, disc1);
        Assert.Single(persistedOrder.ServiceChargeIds, svc1);
        Assert.Equal(2, persistedOrder.PaymentIds.Count);
        Assert.Contains(pay1, persistedOrder.PaymentIds);
        Assert.Contains(pay2, persistedOrder.PaymentIds);
        Assert.Single(persistedTicket.OrderLineIds, line1);
    }

    private static void AssertComparerOrderInvariance<TEntity, TElement>(
        RestaurantDbContext db,
        string propertyName,
        TElement firstItem,
        TElement secondItem)
    {
        var property = db.Model.FindEntityType(typeof(TEntity))!.FindProperty(propertyName)!;
        var comparer = Assert.IsAssignableFrom<ValueComparer<IReadOnlyCollection<TElement>>>(property.GetValueComparer());

        var setA = new HashSet<TElement> { firstItem, secondItem };
        var setB = new HashSet<TElement> { secondItem, firstItem };

        // Ensure runtime enumeration order actually differs between the two equivalent HashSet instances.
        Assert.False(setA.SequenceEqual(setB));
        Assert.True(comparer.Equals(setA, setB));
        Assert.Equal(comparer.GetHashCode(setA), comparer.GetHashCode(setB));
    }
}
