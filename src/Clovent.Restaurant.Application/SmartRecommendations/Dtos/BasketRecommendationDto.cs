namespace Clovent.Restaurant.Application.SmartRecommendations.Dtos;

/// <summary>One suggested add-on for the POS "smart suggestions" panel.</summary>
public sealed record BasketRecommendationDto(
    Guid VariantId,
    string ProductName,
    string VariantName,
    decimal UnitPrice,
    RecommendationReason Reason);
