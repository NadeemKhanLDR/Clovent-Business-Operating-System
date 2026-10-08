namespace Clovent.Restaurant.Orders;

/// <summary>
/// Immutable snapshot of sale-time tax calculation facts captured on an order line.
/// Guarantees that completed transaction history, receipts, refunds, and replays
/// consume fixed historical facts without re-evaluating altered catalog tax tables.
/// </summary>
public sealed record LineTaxSnapshot(
    Guid OrderLineId,
    string TaxClassification,
    string Authority,
    string TaxCode,
    decimal TaxRatePercentage,
    bool TaxIsInclusive,
    decimal Quantity,
    decimal UnitPrice,
    decimal GrossAmount,
    decimal LineDiscountAmount,
    decimal AllocatedOrderDiscountAmount,
    decimal TotalDiscountAmount,
    decimal DiscountedAmount,
    decimal TaxableBase,
    decimal TaxAmount,
    decimal LinePayable,
    string CalculationPolicyVersion = "1.3.0-AwayFromZero-v1");
