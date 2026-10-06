namespace Clovent.Restaurant.Application.Outbox.Dtos;

/// <summary>Payload for asynchronous inventory stock deduction.</summary>
public sealed record InventoryPostingLineItem(Guid ProductVariantId, string Sku, string Name, decimal Quantity);

/// <summary>Payload for <see cref="Clovent.Restaurant.Outbox.OutboxMessageType.InventoryPosting"/>.</summary>
public sealed record InventoryPostingPayload(
    Guid OrderId,
    string OrderNumber,
    Guid WarehouseId,
    IReadOnlyList<InventoryPostingLineItem> Items);

/// <summary>Payload for <see cref="Clovent.Restaurant.Outbox.OutboxMessageType.QuickBooksSync"/>.</summary>
public sealed record QuickBooksSyncPayload(
    Guid OrderId,
    string OrderNumber,
    decimal TotalAmount,
    string PaymentMethod,
    string? CustomerName,
    DateTimeOffset CompletedAtUtc);

/// <summary>Payload for <see cref="Clovent.Restaurant.Outbox.OutboxMessageType.ReceiptPrint"/>.</summary>
public sealed record ReceiptPrintPayload(
    Guid OrderId,
    string OrderNumber,
    string ReceiptText,
    string? TargetPrinter = null);

/// <summary>Payload for <see cref="Clovent.Restaurant.Outbox.OutboxMessageType.CloudSync"/>.</summary>
public sealed record CloudSyncPayload(
    Guid OrderId,
    string OrderNumber,
    decimal TotalAmount,
    Guid BranchId,
    Guid TerminalId,
    DateTimeOffset TimestampUtc);

/// <summary>Payload for <see cref="Clovent.Restaurant.Outbox.OutboxMessageType.AnalyticsEvent"/>.</summary>
public sealed record AnalyticsEventPayload(
    string EventName,
    string AggregateId,
    string Details,
    DateTimeOffset TimestampUtc);

/// <summary>Payload for <see cref="Clovent.Restaurant.Outbox.OutboxMessageType.RecommendationLearning"/>.</summary>
public sealed record RecommendationLearningPayload(
    Guid OrderId,
    IReadOnlyList<Guid> ProductVariantIds,
    DateTimeOffset TimestampUtc);
