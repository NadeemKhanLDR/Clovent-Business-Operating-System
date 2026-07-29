using Clovent.Restaurant;
using Clovent.Restaurant.KitchenTickets;
using Clovent.Restaurant.KitchenTickets.Events;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Xunit;

namespace Clovent.Restaurant.Tests.KitchenTickets;

public class KitchenTicketTests
{
    private static KitchenTicket CreateTicket() => KitchenTicket.Create(OrderId.New(), [OrderLineId.New()]);

    [Fact]
    public void Create_Valid_NewByDefault_RaisesKitchenTicketCreated()
    {
        var ticket = CreateTicket();

        Assert.Equal(KitchenTicketStatus.New, ticket.Status);
        Assert.IsType<KitchenTicketCreated>(Assert.Single(ticket.DomainEvents));
    }

    [Fact]
    public void Create_NoLines_Throws()
    {
        Assert.Throws<ArgumentException>(() => KitchenTicket.Create(OrderId.New(), []));
    }

    [Fact]
    public void Start_FromNew_Succeeds()
    {
        var ticket = CreateTicket();
        ticket.ClearDomainEvents();

        ticket.Start();

        Assert.Equal(KitchenTicketStatus.InProgress, ticket.Status);
        Assert.NotNull(ticket.StartedAtUtc);
        Assert.IsType<KitchenTicketStarted>(Assert.Single(ticket.DomainEvents));
    }

    [Fact]
    public void Start_NotNew_Throws()
    {
        var ticket = CreateTicket();
        ticket.Start();

        Assert.Throws<RestaurantDomainException>(() => ticket.Start());
    }

    [Fact]
    public void MarkReady_FromInProgress_Succeeds()
    {
        var ticket = CreateTicket();
        ticket.Start();
        ticket.ClearDomainEvents();

        ticket.MarkReady();

        Assert.Equal(KitchenTicketStatus.Ready, ticket.Status);
        Assert.NotNull(ticket.ReadyAtUtc);
        Assert.IsType<KitchenTicketMarkedReady>(Assert.Single(ticket.DomainEvents));
    }

    [Fact]
    public void MarkReady_NotInProgress_Throws()
    {
        var ticket = CreateTicket();

        Assert.Throws<RestaurantDomainException>(() => ticket.MarkReady());
    }

    [Fact]
    public void Serve_FromReady_Succeeds()
    {
        var ticket = CreateTicket();
        ticket.Start();
        ticket.MarkReady();
        ticket.ClearDomainEvents();

        ticket.Serve();

        Assert.Equal(KitchenTicketStatus.Served, ticket.Status);
        Assert.NotNull(ticket.ServedAtUtc);
        Assert.IsType<KitchenTicketServed>(Assert.Single(ticket.DomainEvents));
    }

    [Fact]
    public void Serve_NotReady_Throws()
    {
        var ticket = CreateTicket();

        Assert.Throws<RestaurantDomainException>(() => ticket.Serve());
    }

    [Fact]
    public void Cancel_FromNew_Succeeds()
    {
        var ticket = CreateTicket();
        ticket.ClearDomainEvents();

        ticket.Cancel();

        Assert.Equal(KitchenTicketStatus.Cancelled, ticket.Status);
        Assert.IsType<KitchenTicketCancelled>(Assert.Single(ticket.DomainEvents));
    }

    [Fact]
    public void Cancel_AlreadyServed_Throws()
    {
        var ticket = CreateTicket();
        ticket.Start();
        ticket.MarkReady();
        ticket.Serve();

        Assert.Throws<RestaurantDomainException>(() => ticket.Cancel());
    }
}
