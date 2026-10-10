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

/// <summary>Payload for <see cref="Clovent.Restaurant.Outbox.OutboxMessageType.InventoryDeltaSync"/>.</summary>
public sealed record InventoryDeltaSyncPayload(
    Guid TerminalId,
    Guid BranchId,
    Guid WarehouseId,
    Guid ProductVariantId,
    string Sku,
    decimal QuantityDelta,
    string OperationType,
    string? ConcurrencyToken,
    DateTimeOffset TimestampUtc);

/// <summary>Payload for <see cref="Clovent.Restaurant.Outbox.OutboxMessageType.CatalogPriceDeltaSync"/>.</summary>
public sealed record CatalogPriceAdjustmentPayload(
    Guid TerminalId,
    Guid BranchId,
    Guid ProductVariantId,
    string PriceType,
    decimal NewAmount,
    decimal OldAmount,
    string? ConcurrencyToken,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset TimestampUtc,
    Guid CurrencyId = default,
    Guid? UnitOfMeasureId = null,
    string? PriceListName = null,
    DateTimeOffset? EffectiveToUtc = null);

/// <summary>Payload for <see cref="Clovent.Restaurant.Outbox.OutboxMessageType.ShiftSummaryDeltaSync"/>.</summary>
public sealed record ShiftSummaryDeltaSyncPayload(
    Guid TerminalId,
    Guid BranchId,
    int ShiftNumber,
    Guid CashierId,
    string CashierName,
    DateTimeOffset OpenedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    decimal StartingCash,
    decimal CountedCash,
    decimal ExpectedCash,
    decimal CashVariance,
    decimal NetSales,
    int TotalOrdersCount,
    DateTimeOffset TimestampUtc);

/// <summary>Payload envelope wrapping a generic <see cref="Clovent.Platform.Sync.SyncPacket"/> in the outbox.</summary>
public sealed record DeltaSyncPacketEnvelopePayload(
    Clovent.Platform.Sync.SyncPacket Packet);

/// <summary>Payload for store manager notifications regarding cashier behavior anomalies.</summary>
public sealed record CashierAuditAlertOutboxPayload(
    Guid AlertId,
    string CashierName,
    string AnomalyType,
    string Severity,
    decimal RiskScore,
    string Description,
    DateTimeOffset DetectedAtUtc,
    string? SuspectDetailsJson,
    Guid? ShiftId = null,
    Guid? OrderId = null);


