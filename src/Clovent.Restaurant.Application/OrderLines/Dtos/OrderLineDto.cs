using Clovent.Restaurant.OrderLines;

namespace Clovent.Restaurant.Application.OrderLines.Dtos;

/// <summary>Read-model shape for an <see cref="OrderLine"/>, safe to cross a process boundary.</summary>
public sealed record OrderLineDto(
    Guid OrderLineId,
    Guid OrderId,
    Guid ProductVariantId,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRatePercentage,
    bool TaxIsInclusive,
    string? Notes,
    bool IsVoided,
    decimal LineTotal,
    DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="OrderLine"/> into its DTO.</summary>
    public static OrderLineDto FromDomain(OrderLine line) => new(
        line.Id.Value,
        line.OrderId.Value,
        line.ProductVariantId.Value,
        line.Quantity,
        line.UnitPrice,
        line.TaxRatePercentage,
        line.TaxIsInclusive,
        line.Notes,
        line.IsVoided,
        line.LineTotal,
        line.CreatedAtUtc);
}
