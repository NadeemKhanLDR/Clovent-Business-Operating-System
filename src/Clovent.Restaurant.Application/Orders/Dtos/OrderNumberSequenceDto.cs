using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.Application.Orders.Dtos;

/// <summary>Read model for the Restaurant Setup screen's order-number configuration.</summary>
public sealed record OrderNumberSequenceDto(string Prefix, int NextNumber)
{
    /// <summary>Projects a domain <see cref="OrderNumberSequence"/> into its DTO.</summary>
    public static OrderNumberSequenceDto FromDomain(OrderNumberSequence sequence) => new(sequence.Prefix, sequence.NextNumber);
}
