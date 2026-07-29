using Clovent.Restaurant.KitchenTickets;

namespace Clovent.Restaurant.Application.KitchenTickets.Dtos;

/// <summary>Read-model shape for a <see cref="KitchenTicket"/>, safe to cross a process boundary.</summary>
public sealed record KitchenTicketDto(
    Guid KitchenTicketId,
    Guid OrderId,
    IReadOnlyCollection<Guid> OrderLineIds,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? ReadyAtUtc,
    DateTimeOffset? ServedAtUtc)
{
    /// <summary>Projects a domain <see cref="KitchenTicket"/> into its DTO.</summary>
    public static KitchenTicketDto FromDomain(KitchenTicket ticket) => new(
        ticket.Id.Value,
        ticket.OrderId.Value,
        [.. ticket.OrderLineIds.Select(id => id.Value)],
        ticket.Status.ToString(),
        ticket.CreatedAtUtc,
        ticket.StartedAtUtc,
        ticket.ReadyAtUtc,
        ticket.ServedAtUtc);
}
