using Clovent.Catalog.Variants;
using Clovent.Restaurant;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.OrderLines.Events;
using Clovent.Restaurant.Orders;
using Xunit;

namespace Clovent.Restaurant.Tests.OrderLines;

public class OrderLineTests
{
    private static OrderLine CreateLine(decimal quantity = 2, decimal unitPrice = 9.99m) =>
        OrderLine.Create(OrderId.New(), ProductVariantId.New(), quantity, unitPrice, taxRatePercentage: 15m, taxIsInclusive: false);

    [Fact]
    public void Create_Valid_NotVoidedByDefault_RaisesOrderLineCreated()
    {
        var line = CreateLine();

        Assert.False(line.IsVoided);
        Assert.IsType<OrderLineCreated>(Assert.Single(line.DomainEvents));
    }

    [Fact]
    public void Create_NonPositiveQuantity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OrderLine.Create(OrderId.New(), ProductVariantId.New(), 0, 9.99m, 0, false));
    }

    [Fact]
    public void Create_NegativePrice_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OrderLine.Create(OrderId.New(), ProductVariantId.New(), 1, -1m, 0, false));
    }

    [Fact]
    public void LineTotal_IsQuantityTimesUnitPrice()
    {
        var line = CreateLine(quantity: 3, unitPrice: 5m);

        Assert.Equal(15m, line.LineTotal);
    }

    [Fact]
    public void SetQuantity_Different_RaisesOrderLineQuantityChanged()
    {
        var line = CreateLine();
        line.ClearDomainEvents();

        line.SetQuantity(5);

        Assert.Equal(5, line.Quantity);
        Assert.IsType<OrderLineQuantityChanged>(Assert.Single(line.DomainEvents));
    }

    [Fact]
    public void SetQuantity_NonPositive_Throws()
    {
        var line = CreateLine();

        Assert.Throws<ArgumentOutOfRangeException>(() => line.SetQuantity(0));
    }

    [Fact]
    public void SetNotes_DifferentValue_RaisesOrderLineNotesChanged()
    {
        var line = CreateLine();
        line.ClearDomainEvents();

        line.SetNotes("No onions");

        Assert.Equal("No onions", line.Notes);
        Assert.IsType<OrderLineNotesChanged>(Assert.Single(line.DomainEvents));
    }

    [Fact]
    public void Void_ThenUnvoid_RaisesExpectedEvents()
    {
        var line = CreateLine();
        line.ClearDomainEvents();

        line.Void();
        Assert.True(line.IsVoided);
        Assert.IsType<OrderLineVoided>(Assert.Single(line.DomainEvents));

        line.ClearDomainEvents();
        line.Unvoid();
        Assert.False(line.IsVoided);
        Assert.IsType<OrderLineUnvoided>(Assert.Single(line.DomainEvents));
    }

    [Fact]
    public void Void_AlreadyVoided_Throws()
    {
        var line = CreateLine();
        line.Void();

        Assert.Throws<RestaurantDomainException>(() => line.Void());
    }

    [Fact]
    public void Unvoid_NotVoided_Throws()
    {
        var line = CreateLine();

        Assert.Throws<RestaurantDomainException>(() => line.Unvoid());
    }

    [Fact]
    public void TransferToOrder_DifferentOrder_RaisesOrderLineTransferredToOrder()
    {
        var line = CreateLine();
        line.ClearDomainEvents();
        var newOrderId = OrderId.New();

        line.TransferToOrder(newOrderId);

        Assert.Equal(newOrderId, line.OrderId);
        Assert.IsType<OrderLineTransferredToOrder>(Assert.Single(line.DomainEvents));
    }

    [Fact]
    public void TransferToOrder_SameOrder_NoEventRaised()
    {
        var line = CreateLine();
        var sameOrderId = line.OrderId;
        line.ClearDomainEvents();

        line.TransferToOrder(sameOrderId);

        Assert.Empty(line.DomainEvents);
    }
}
