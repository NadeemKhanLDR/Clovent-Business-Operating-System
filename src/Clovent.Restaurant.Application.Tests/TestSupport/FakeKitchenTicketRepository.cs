using Clovent.Restaurant.KitchenTickets;
using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class FakeKitchenTicketRepository : IKitchenTicketRepository
{
    private readonly Dictionary<KitchenTicketId, KitchenTicket> _tickets = [];

    public void Add(KitchenTicket ticket) => _tickets[ticket.Id] = ticket;

    public Task<KitchenTicket?> GetByIdAsync(KitchenTicketId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_tickets.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<KitchenTicket>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<KitchenTicket>>([.. _tickets.Values.Where(t => t.OrderId == orderId)]);

    public Task<IReadOnlyCollection<KitchenTicket>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<KitchenTicket>>([.. _tickets.Values.Where(t => t.Status is not (KitchenTicketStatus.Served or KitchenTicketStatus.Cancelled))]);

    public Task AddAsync(KitchenTicket kitchenTicket, CancellationToken cancellationToken = default)
    {
        _tickets[kitchenTicket.Id] = kitchenTicket;
        return Task.CompletedTask;
    }
}
