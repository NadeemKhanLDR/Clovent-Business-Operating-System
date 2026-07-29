using Clovent.Restaurant.Discounts;

namespace Clovent.Restaurant.Application.Discounts.Dtos;

/// <summary>Read-model shape for a <see cref="Discount"/>, safe to cross a process boundary.</summary>
public sealed record DiscountDto(Guid DiscountId, Guid OrderId, string DiscountType, decimal Value, string Reason, DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="Discount"/> into its DTO.</summary>
    public static DiscountDto FromDomain(Discount discount) => new(
        discount.Id.Value, discount.OrderId.Value, discount.DiscountType.ToString(), discount.Value, discount.Reason, discount.CreatedAtUtc);
}
