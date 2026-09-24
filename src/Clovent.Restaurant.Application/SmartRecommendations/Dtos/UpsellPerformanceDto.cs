namespace Clovent.Restaurant.Application.SmartRecommendations.Dtos;

/// <summary>
/// One row of the Back Office Upsell Performance grid: how often a
/// recommended variant was offered, accepted and dismissed, the resulting
/// conversion rate, and the revenue attributed to suggestion interactions.
/// </summary>
/// <param name="VariantId">The recommended variant.</param>
/// <param name="ProductName">Product display name resolved from the catalog.</param>
/// <param name="VariantName">Variant display name resolved from the catalog.</param>
/// <param name="Offers">Offered events in the queried interval.</param>
/// <param name="Accepted">Accepted events in the queried interval.</param>
/// <param name="Dismissed">Dismissed events in the queried interval.</param>
/// <param name="ConversionPercent">Accepted / Offered, rounded to one decimal.</param>
/// <param name="UpsellRevenue">Sum of quantity x unit amount over accepted events.</param>
public sealed record UpsellPerformanceDto(
    Guid VariantId,
    string ProductName,
    string VariantName,
    int Offers,
    int Accepted,
    int Dismissed,
    decimal ConversionPercent,
    decimal UpsellRevenue);
