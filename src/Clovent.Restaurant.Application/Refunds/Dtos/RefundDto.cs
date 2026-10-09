using Clovent.Restaurant.Refunds;

namespace Clovent.Restaurant.Application.Refunds.Dtos;

/// <summary>Read-model shape for a refund line item.</summary>
public sealed record RefundLineDto(
    Guid RefundLineId,
    Guid OrderLineId,
    Guid ProductVariantId,
    string Sku,
    string Name,
    decimal Quantity,
    decimal UnitPrice,
    decimal GrossAmount,
    decimal DiscountReversed,
    decimal TaxReversed,
    decimal LineTotalRefunded,
    string TaxClassification,
    string TaxCode,
    decimal TaxRatePercentage,
    bool TaxIsInclusive,
    string InventoryDisposition);

/// <summary>Read-model shape for a completed refund credit note document.</summary>
public sealed record RefundDto(
    Guid RefundId,
    string RefundNumber,
    Guid OrderId,
    Guid WarehouseId,
    DateTimeOffset RefundedAtUtc,
    Guid CashierId,
    string CashierName,
    string Reason,
    Guid? ApprovedByUserId,
    string? ApprovedByUserName,
    string IdempotencyKey,
    decimal SubtotalRefunded,
    decimal DiscountReversedTotal,
    decimal TaxReversedTotal,
    decimal GrandTotalRefunded,
    string SettlementMethod,
    string? SettlementReference,
    Guid? CustomerId,
    string? ReceiptSnapshotJson,
    IReadOnlyList<RefundLineDto> Lines)
{
    /// <summary>Projects domain Refund to DTO.</summary>
    public static RefundDto FromDomain(Refund refund) => new(
        refund.Id.Value,
        refund.RefundNumber.Value,
        refund.OrderId.Value,
        refund.WarehouseId.Value,
        refund.RefundedAtUtc,
        refund.CashierId,
        refund.CashierName,
        refund.Reason,
        refund.ApprovedByUserId,
        refund.ApprovedByUserName,
        refund.IdempotencyKey,
        refund.SubtotalRefunded,
        refund.DiscountReversedTotal,
        refund.TaxReversedTotal,
        refund.GrandTotalRefunded,
        refund.SettlementMethod.ToString(),
        refund.SettlementReference,
        refund.CustomerId,
        refund.ReceiptSnapshotJson,
        refund.Lines.Select(l => new RefundLineDto(
            l.Id.Value,
            l.OrderLineId.Value,
            l.ProductVariantId.Value,
            l.Sku,
            l.Name,
            l.Quantity,
            l.UnitPrice,
            l.GrossAmount,
            l.DiscountReversed,
            l.TaxReversed,
            l.LineTotalRefunded,
            l.TaxClassification,
            l.TaxCode,
            l.TaxRatePercentage,
            l.TaxIsInclusive,
            l.InventoryDisposition.ToString())).ToList());
}
