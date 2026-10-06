using System.Text.Json;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Clovent.Restaurant.Outbox;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Queries;

/// <summary>DTO representing a completed order whose inventory stock deduction has not yet posted.</summary>
public sealed record UnpostedInventoryOrderDto(
    Guid OrderId,
    string OrderNumber,
    Guid WarehouseId,
    int ItemCount,
    string OutboxStatus,
    int AttemptCount,
    DateTimeOffset CreatedAtUtc,
    string? LastError);

/// <summary>Query returning all completed orders whose inventory deduction outbox jobs are pending, processing, or failed.</summary>
public sealed record GetUnpostedInventoryOrdersQuery : IRequest<IReadOnlyList<UnpostedInventoryOrderDto>>;

/// <summary>Handles <see cref="GetUnpostedInventoryOrdersQuery"/>.</summary>
public sealed class GetUnpostedInventoryOrdersQueryHandler(
    IOutboxRepository outboxRepository) : IRequestHandler<GetUnpostedInventoryOrdersQuery, IReadOnlyList<UnpostedInventoryOrderDto>>
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    /// <inheritdoc/>
    public async Task<IReadOnlyList<UnpostedInventoryOrderDto>> Handle(
        GetUnpostedInventoryOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var unpostedMessages = await outboxRepository
            .GetUncompletedMessagesByTypeAsync(OutboxMessageType.InventoryPosting, 200, cancellationToken)
            .ConfigureAwait(false);

        var result = new List<UnpostedInventoryOrderDto>(unpostedMessages.Count);
        foreach (var msg in unpostedMessages)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<InventoryPostingPayload>(msg.Payload, JsonOpts);
                if (payload != null)
                {
                    result.Add(new UnpostedInventoryOrderDto(
                        payload.OrderId,
                        payload.OrderNumber,
                        payload.WarehouseId,
                        payload.Items?.Count ?? 0,
                        msg.Status.ToString(),
                        msg.AttemptCount,
                        msg.CreatedAtUtc,
                        msg.LastError));
                }
            }
            catch
            {
                // Fallback with aggregate ID
                if (Guid.TryParse(msg.AggregateId, out var orderId))
                {
                    result.Add(new UnpostedInventoryOrderDto(
                        orderId,
                        $"ORD-{msg.AggregateId[..8]}",
                        Guid.Empty,
                        0,
                        msg.Status.ToString(),
                        msg.AttemptCount,
                        msg.CreatedAtUtc,
                        msg.LastError));
                }
            }
        }

        return result.AsReadOnly();
    }
}
