using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.Application.Orders.Dtos;

/// <summary>Read-model shape for an <see cref="Order"/>, safe to cross a process boundary.</summary>
public sealed record OrderDto(
    Guid OrderId,
    string OrderNumber,
    int? DailySalesNumber,
    string OrderType,
    string Status,
    Guid? TableId,
    Guid WarehouseId,
    string? Notes,
    string? CustomerNotes,
    IReadOnlyCollection<Guid> OrderLineIds,
    IReadOnlyCollection<Guid> DiscountIds,
    IReadOnlyCollection<Guid> ServiceChargeIds,
    IReadOnlyCollection<Guid> PaymentIds,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    /// <summary>Projects a domain <see cref="Order"/> into its DTO.</summary>
    public static OrderDto FromDomain(Order order) => new(
        order.Id.Value,
        order.OrderNumber.Value,
        order.DailySalesNumber,
        order.OrderType.ToString(),
        order.Status.ToString(),
        order.TableId?.Value,
        order.WarehouseId.Value,
        order.Notes,
        order.CustomerNotes,
        [.. order.OrderLineIds.Select(id => id.Value)],
        [.. order.DiscountIds.Select(id => id.Value)],
        [.. order.ServiceChargeIds.Select(id => id.Value)],
        [.. order.PaymentIds.Select(id => id.Value)],
        order.CreatedAtUtc,
        order.UpdatedAtUtc);
}
