using Clovent.Restaurant.ServiceCharges;

namespace Clovent.Restaurant.Application.ServiceCharges.Dtos;

/// <summary>Read-model shape for a <see cref="ServiceCharge"/>, safe to cross a process boundary.</summary>
public sealed record ServiceChargeDto(Guid ServiceChargeId, Guid OrderId, string ServiceChargeType, decimal Value, string Reason, DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="ServiceCharge"/> into its DTO.</summary>
    public static ServiceChargeDto FromDomain(ServiceCharge charge) => new(
        charge.Id.Value, charge.OrderId.Value, charge.ServiceChargeType.ToString(), charge.Value, charge.Reason, charge.CreatedAtUtc);
}
