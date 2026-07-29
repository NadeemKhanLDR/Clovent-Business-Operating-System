using Clovent.Restaurant.PaymentMethods;

namespace Clovent.Restaurant.Application.PaymentMethods.Dtos;

/// <summary>Read-model shape for a <see cref="PaymentMethod"/>, safe to cross a process boundary.</summary>
public sealed record PaymentMethodDto(Guid PaymentMethodId, string Name, string Status, DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="PaymentMethod"/> into its DTO.</summary>
    public static PaymentMethodDto FromDomain(PaymentMethod method) => new(
        method.Id.Value, method.Name.Value, method.Status.ToString(), method.CreatedAtUtc);
}
