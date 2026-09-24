namespace Clovent.Restaurant.Application.CustomerReorder.Dtos;

/// <summary>One line of a customer's previous or habitual order, resolved against the current catalog.</summary>
public sealed record CustomerReorderLineDto(
    Guid VariantId,
    string ProductName,
    string VariantName,
    decimal Quantity,
    decimal UnitPrice,
    bool IsAvailable);

/// <summary>A customer's most recent order, ready to be replayed as new order lines.</summary>
public sealed record CustomerReorderDto(
    Guid OrderId,
    string OrderNumber,
    DateTimeOffset OrderDateUtc,
    IReadOnlyCollection<CustomerReorderLineDto> Lines);
