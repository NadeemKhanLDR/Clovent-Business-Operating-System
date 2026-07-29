using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.KitchenTickets;
using Clovent.Restaurant.Orders;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IKitchenTicketRepository"/>.</summary>
public sealed class KitchenTicketRepository(RestaurantDbContext dbContext) : IKitchenTicketRepository
{
    /// <inheritdoc/>
    public Task<KitchenTicket?> GetByIdAsync(KitchenTicketId id, CancellationToken cancellationToken = default) =>
        dbContext.KitchenTickets.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<KitchenTicket>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default) =>
        await dbContext.KitchenTickets.Where(t => t.OrderId == orderId).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<KitchenTicket>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        await dbContext.KitchenTickets
            .Where(t => t.Status != KitchenTicketStatus.Served && t.Status != KitchenTicketStatus.Cancelled)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(KitchenTicket kitchenTicket, CancellationToken cancellationToken = default) =>
        await dbContext.KitchenTickets.AddAsync(kitchenTicket, cancellationToken);
}
