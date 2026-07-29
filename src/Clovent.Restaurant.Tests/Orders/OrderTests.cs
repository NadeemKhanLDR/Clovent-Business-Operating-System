using Clovent.MasterData.Warehouses;
using Clovent.Restaurant;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Orders.Events;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.ServiceCharges;
using Clovent.Restaurant.Tables;
using Xunit;

namespace Clovent.Restaurant.Tests.Orders;

public class OrderTests
{
    private static Order CreateDineInOrder() => Order.Create(OrderType.DineIn, WarehouseId.New(), TableId.New());

    private static Order CreateTakeAwayOrder() => Order.Create(OrderType.TakeAway, WarehouseId.New());

    [Fact]
    public void Create_DineIn_Valid_RaisesOrderCreated()
    {
        var order = CreateDineInOrder();

        Assert.Equal(OrderType.DineIn, order.OrderType);
        Assert.Equal(OrderStatus.Open, order.Status);
        Assert.NotNull(order.TableId);
        Assert.IsType<OrderCreated>(Assert.Single(order.DomainEvents));
    }

    [Fact]
    public void Create_DineIn_NoTable_Throws()
    {
        Assert.Throws<RestaurantDomainException>(() => Order.Create(OrderType.DineIn, WarehouseId.New()));
    }

    [Fact]
    public void Create_TakeAway_WithTable_Throws()
    {
        Assert.Throws<RestaurantDomainException>(() => Order.Create(OrderType.TakeAway, WarehouseId.New(), TableId.New()));
    }

    [Fact]
    public void Create_TakeAway_Valid_NoTable()
    {
        var order = CreateTakeAwayOrder();

        Assert.Null(order.TableId);
    }

    [Fact]
    public void AddOrderLine_WhileOpen_Succeeds()
    {
        var order = CreateTakeAwayOrder();
        order.ClearDomainEvents();
        var lineId = OrderLineId.New();

        order.AddOrderLine(lineId);

        Assert.Contains(lineId, order.OrderLineIds);
        Assert.IsType<OrderLineAdded>(Assert.Single(order.DomainEvents));
    }

    [Fact]
    public void AddOrderLine_Duplicate_Throws()
    {
        var order = CreateTakeAwayOrder();
        var lineId = OrderLineId.New();
        order.AddOrderLine(lineId);

        Assert.Throws<RestaurantDomainException>(() => order.AddOrderLine(lineId));
    }

    [Fact]
    public void AddOrderLine_WhileHeld_Throws()
    {
        var order = CreateTakeAwayOrder();
        order.AddOrderLine(OrderLineId.New());
        order.Hold();

        Assert.Throws<RestaurantDomainException>(() => order.AddOrderLine(OrderLineId.New()));
    }

    [Fact]
    public void RemoveOrderLine_NotOnOrder_Throws()
    {
        var order = CreateTakeAwayOrder();

        Assert.Throws<RestaurantDomainException>(() => order.RemoveOrderLine(OrderLineId.New()));
    }

    [Fact]
    public void RemoveOrderLine_OnOrder_Succeeds()
    {
        var order = CreateTakeAwayOrder();
        var lineId = OrderLineId.New();
        order.AddOrderLine(lineId);
        order.ClearDomainEvents();

        order.RemoveOrderLine(lineId);

        Assert.DoesNotContain(lineId, order.OrderLineIds);
        Assert.IsType<OrderLineRemoved>(Assert.Single(order.DomainEvents));
    }

    [Fact]
    public void ApplyDiscount_ThenRemove_RaisesExpectedEvents()
    {
        var order = CreateTakeAwayOrder();
        order.ClearDomainEvents();
        var discountId = DiscountId.New();

        order.ApplyDiscount(discountId);
        Assert.Contains(discountId, order.DiscountIds);
        Assert.IsType<OrderDiscountApplied>(Assert.Single(order.DomainEvents));

        order.ClearDomainEvents();
        order.RemoveDiscount(discountId);
        Assert.DoesNotContain(discountId, order.DiscountIds);
        Assert.IsType<OrderDiscountRemoved>(Assert.Single(order.DomainEvents));
    }

    [Fact]
    public void ApplyServiceCharge_ThenRemove_RaisesExpectedEvents()
    {
        var order = CreateTakeAwayOrder();
        order.ClearDomainEvents();
        var serviceChargeId = ServiceChargeId.New();

        order.ApplyServiceCharge(serviceChargeId);
        Assert.Contains(serviceChargeId, order.ServiceChargeIds);
        Assert.IsType<OrderServiceChargeApplied>(Assert.Single(order.DomainEvents));

        order.ClearDomainEvents();
        order.RemoveServiceCharge(serviceChargeId);
        Assert.DoesNotContain(serviceChargeId, order.ServiceChargeIds);
        Assert.IsType<OrderServiceChargeRemoved>(Assert.Single(order.DomainEvents));
    }

    [Fact]
    public void RecordPayment_WhileOpen_Succeeds()
    {
        var order = CreateTakeAwayOrder();
        order.ClearDomainEvents();
        var paymentId = PaymentId.New();

        order.RecordPayment(paymentId);

        Assert.Contains(paymentId, order.PaymentIds);
        Assert.IsType<OrderPaymentRecorded>(Assert.Single(order.DomainEvents));
    }

    [Fact]
    public void SetNotes_DifferentValue_RaisesOrderNotesChanged()
    {
        var order = CreateTakeAwayOrder();
        order.ClearDomainEvents();

        order.SetNotes("Extra napkins");

        Assert.Equal("Extra napkins", order.Notes);
        Assert.IsType<OrderNotesChanged>(Assert.Single(order.DomainEvents));
    }

    [Fact]
    public void SetCustomerNotes_DifferentValue_RaisesOrderCustomerNotesChanged()
    {
        var order = CreateTakeAwayOrder();
        order.ClearDomainEvents();

        order.SetCustomerNotes("Birthday celebration");

        Assert.Equal("Birthday celebration", order.CustomerNotes);
        Assert.IsType<OrderCustomerNotesChanged>(Assert.Single(order.DomainEvents));
    }

    [Fact]
    public void AssignTable_DineIn_ChangesTable_RaisesOrderTableAssigned()
    {
        var order = CreateDineInOrder();
        order.ClearDomainEvents();
        var newTableId = TableId.New();

        order.AssignTable(newTableId);

        Assert.Equal(newTableId, order.TableId);
        Assert.IsType<OrderTableAssigned>(Assert.Single(order.DomainEvents));
    }

    [Fact]
    public void AssignTable_TakeAway_Throws()
    {
        var order = CreateTakeAwayOrder();

        Assert.Throws<RestaurantDomainException>(() => order.AssignTable(TableId.New()));
    }

    [Fact]
    public void Hold_ThenResume_RaisesExpectedEvents()
    {
        var order = CreateTakeAwayOrder();
        order.ClearDomainEvents();

        order.Hold();
        Assert.Equal(OrderStatus.Held, order.Status);
        Assert.IsType<OrderHeld>(Assert.Single(order.DomainEvents));

        order.ClearDomainEvents();
        order.Resume();
        Assert.Equal(OrderStatus.Open, order.Status);
        Assert.IsType<OrderResumed>(Assert.Single(order.DomainEvents));
    }

    [Fact]
    public void Resume_NotHeld_Throws()
    {
        var order = CreateTakeAwayOrder();

        Assert.Throws<RestaurantDomainException>(() => order.Resume());
    }

    [Fact]
    public void Void_FromOpen_Succeeds()
    {
        var order = CreateTakeAwayOrder();
        order.ClearDomainEvents();

        order.Void("Customer complaint");

        Assert.Equal(OrderStatus.Voided, order.Status);
        Assert.IsType<OrderVoided>(Assert.Single(order.DomainEvents));
    }

    [Fact]
    public void Void_FromCompleted_Succeeds()
    {
        var order = CreateTakeAwayOrder();
        order.Complete();
        order.ClearDomainEvents();

        order.Void("Managerial override");

        Assert.Equal(OrderStatus.Voided, order.Status);
    }

    [Fact]
    public void Void_EmptyReason_Throws()
    {
        var order = CreateTakeAwayOrder();

        Assert.Throws<ArgumentException>(() => order.Void(" "));
    }

    [Fact]
    public void Void_AlreadyVoided_Throws()
    {
        var order = CreateTakeAwayOrder();
        order.Void("First void");

        Assert.Throws<RestaurantDomainException>(() => order.Void("Second void"));
    }

    [Fact]
    public void Cancel_FromOpen_NoPayments_Succeeds()
    {
        var order = CreateTakeAwayOrder();
        order.ClearDomainEvents();

        order.Cancel("Customer left");

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.IsType<OrderCancelled>(Assert.Single(order.DomainEvents));
    }

    [Fact]
    public void Cancel_WithPayments_Throws()
    {
        var order = CreateTakeAwayOrder();
        order.RecordPayment(PaymentId.New());

        Assert.Throws<RestaurantDomainException>(() => order.Cancel("Attempted cancel"));
    }

    [Fact]
    public void Reopen_FromVoided_ReturnsToOpen()
    {
        var order = CreateTakeAwayOrder();
        order.Void("Mistake");
        order.ClearDomainEvents();

        order.Reopen();

        Assert.Equal(OrderStatus.Open, order.Status);
        Assert.IsType<OrderReopened>(Assert.Single(order.DomainEvents));
    }

    [Fact]
    public void Reopen_FromCancelled_ReturnsToOpen()
    {
        var order = CreateTakeAwayOrder();
        order.Cancel("Mistake");

        order.Reopen();

        Assert.Equal(OrderStatus.Open, order.Status);
    }

    [Fact]
    public void Reopen_FromOpen_Throws()
    {
        var order = CreateTakeAwayOrder();

        Assert.Throws<RestaurantDomainException>(() => order.Reopen());
    }

    [Fact]
    public void Complete_FromOpen_Succeeds()
    {
        var order = CreateTakeAwayOrder();
        order.ClearDomainEvents();

        order.Complete();

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.IsType<OrderCompleted>(Assert.Single(order.DomainEvents));
    }

    [Fact]
    public void Complete_NotOpen_Throws()
    {
        var order = CreateTakeAwayOrder();
        order.Hold();

        Assert.Throws<RestaurantDomainException>(() => order.Complete());
    }
}
