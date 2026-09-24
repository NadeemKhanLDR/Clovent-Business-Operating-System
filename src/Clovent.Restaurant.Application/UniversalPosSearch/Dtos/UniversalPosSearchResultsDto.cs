namespace Clovent.Restaurant.Application.UniversalPosSearch.Dtos;

/// <summary>A product hit in universal POS search - a sellable variant with its resolved price and category.</summary>
public sealed record UniversalPosProductDto(
    Guid VariantId,
    string ProductName,
    string VariantName,
    decimal UnitPrice,
    string? CategoryName);

/// <summary>A customer hit in universal POS search.</summary>
public sealed record UniversalPosCustomerDto(
    Guid CustomerId,
    string Code,
    string Name,
    string? Phone);

/// <summary>An order hit in universal POS search.</summary>
public sealed record UniversalPosOrderDto(
    Guid OrderId,
    string OrderNumber,
    string OrderType,
    decimal TotalAmount,
    string Status);

/// <summary>A table hit in universal POS search.</summary>
public sealed record UniversalPosTableDto(
    Guid TableId,
    string TableName,
    string? AreaName,
    bool HasOpenOrder);

/// <summary>The combined result of one universal POS search, each category capped (default 5 hits).</summary>
public sealed record UniversalPosSearchResultsDto(
    IReadOnlyCollection<UniversalPosProductDto> Products,
    IReadOnlyCollection<UniversalPosCustomerDto> Customers,
    IReadOnlyCollection<UniversalPosOrderDto> Orders,
    IReadOnlyCollection<UniversalPosTableDto> Tables);
