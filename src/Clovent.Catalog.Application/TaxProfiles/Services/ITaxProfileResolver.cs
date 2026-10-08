using Clovent.Catalog.TaxProfiles;

namespace Clovent.Catalog.Application.TaxProfiles.Services;

/// <summary>
/// Result of resolving the applicable tax profile and statutory rules for an item.
/// </summary>
public sealed record TaxResolutionResult(
    TaxProfileId? ProfileId,
    string TaxCode,
    string InvoiceDisplayName,
    string Authority,
    string Jurisdiction,
    TaxClassification Classification,
    ItemTaxClassification ItemClassification,
    decimal RatePercentage,
    bool IsInclusive,
    string? PaymentMethodRestriction = null,
    string? ReferenceDocument = null);

/// <summary>
/// Service resolving tax profile precedence and item taxability under Pakistan sales tax regimes.
/// Enforces: Product Profile -> Category Default -> Jurisdiction/Classification Default.
/// Prevents taxable items from silently falling back to zero tax.
/// </summary>
public interface ITaxProfileResolver
{
    /// <summary>
    /// Resolves the effective tax profile for an item given its product, category, and operating jurisdiction.
    /// </summary>
    Task<TaxResolutionResult> ResolveProfileAsync(
        Guid productId,
        Guid? categoryId,
        string itemType,
        string jurisdiction,
        DateTimeOffset effectiveAtUtc,
        string? tenderType = null,
        bool isPaymentTenderVerified = false,
        CancellationToken cancellationToken = default);
}
