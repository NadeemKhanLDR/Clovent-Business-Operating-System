using Clovent.Restaurant.OrderLines;

namespace Clovent.Restaurant.Application.OrderLines.Dtos;

/// <summary>Read-model shape for an <see cref="OrderLine"/>, safe to cross a process boundary.</summary>
public sealed record OrderLineDto(
    Guid OrderLineId,
    Guid OrderId,
    Guid ProductVariantId,
    decimal Quantity,
    decimal UnitPrice,
    decimal OriginalUnitPrice,
    bool IsPriceOverridden,
    string? PriceOverrideReason,
    string? PriceOverriddenBy,
    DateTimeOffset? PriceOverriddenAtUtc,
    decimal TaxRatePercentage,
    bool TaxIsInclusive,
    string? Notes,
    bool IsVoided,
    decimal LineTotal,
    DateTimeOffset CreatedAtUtc,
    string TaxClassification = "Taxable",
    string TaxAuthority = "PRA",
    string TaxCode = "PK-PRA-16",
    decimal? TaxableBase = null,
    decimal AllocatedDiscount = 0m,
    decimal? TaxAmount = null,
    string? CalculationPolicyVersion = null)
{
    /// <summary>Convenience alias for OrderLineId.</summary>
    public Guid Id => OrderLineId;

    /// <summary>Projects a domain <see cref="OrderLine"/> into its DTO.</summary>
    public static OrderLineDto FromDomain(OrderLine line) => new(
        line.Id.Value,
        line.OrderId.Value,
        line.ProductVariantId.Value,
        line.Quantity,
        line.UnitPrice,
        line.OriginalUnitPrice,
        line.IsPriceOverridden,
        line.PriceOverrideReason,
        line.PriceOverriddenBy,
        line.PriceOverriddenAtUtc,
        line.TaxRatePercentage,
        line.TaxIsInclusive,
        line.Notes,
        line.IsVoided,
        line.LineTotal,
        line.CreatedAtUtc,
        line.TaxClassification,
        line.TaxAuthority,
        line.TaxCode,
        line.TaxableBase,
        line.AllocatedDiscount,
        line.TaxAmount,
        line.CalculationPolicyVersion);
}
