using Clovent.Catalog.TaxProfiles;

namespace Clovent.Catalog.Application.TaxProfiles.Dtos;

/// <summary>Read-model representation of a statutory sales tax profile.</summary>
public sealed record TaxProfileDto(
    Guid TaxProfileId,
    string Code,
    string InvoiceDisplayName,
    string Authority,
    string Jurisdiction,
    string TaxClassification,
    string ItemClassification,
    decimal RatePercentage,
    string PricingMode,
    bool IsInclusive,
    bool IsActive,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset? EffectiveToUtc,
    string? ReferenceDocument,
    string? PaymentMethodRestriction,
    string? Notes)
{
    /// <summary>Projects a domain TaxProfile aggregate into a DTO.</summary>
    public static TaxProfileDto FromDomain(TaxProfile profile) => new(
        profile.Id.Value,
        profile.Code,
        profile.InvoiceDisplayName,
        profile.Authority,
        profile.Jurisdiction,
        profile.TaxClassification.ToString(),
        profile.ItemClassification.ToString(),
        profile.RatePercentage,
        profile.PricingMode.ToString(),
        profile.IsInclusive,
        profile.IsActive,
        profile.EffectiveFromUtc,
        profile.EffectiveToUtc,
        profile.ReferenceDocument,
        profile.PaymentMethodRestriction,
        profile.Notes);
}
