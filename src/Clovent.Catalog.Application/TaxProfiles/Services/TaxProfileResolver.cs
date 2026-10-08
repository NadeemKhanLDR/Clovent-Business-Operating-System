using Clovent.Catalog.Categories;
using Clovent.Catalog.Products;
using Clovent.Catalog.TaxProfiles;

namespace Clovent.Catalog.Application.TaxProfiles.Services;

/// <summary>
/// Authoritative tax profile resolver implementing Pakistan sales tax rules and override precedence:
/// 1. Product explicit profile
/// 2. Category default profile
/// 3. Jurisdiction / Classification default profile
/// 
/// Strictly prevents taxable items from silently falling back to zero tax when configuration is missing.
/// Prevents conflicting active rules and ensures statutory separation between goods and services.
/// </summary>
public sealed class TaxProfileResolver : ITaxProfileResolver
{
    private readonly ITaxProfileRepository _taxProfileRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductCategoryRepository _categoryRepository;

    /// <summary>Initializes a new instance of <see cref="TaxProfileResolver"/>.</summary>
    public TaxProfileResolver(
        ITaxProfileRepository taxProfileRepository,
        IProductRepository productRepository,
        IProductCategoryRepository categoryRepository)
    {
        _taxProfileRepository = taxProfileRepository;
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    /// <inheritdoc/>
    public async Task<TaxResolutionResult> ResolveProfileAsync(
        Guid productId,
        Guid? categoryId,
        string itemType,
        string jurisdiction,
        DateTimeOffset effectiveAtUtc,
        string? tenderType = null,
        bool isPaymentTenderVerified = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jurisdiction))
        {
            throw new ArgumentException("Jurisdiction is required to resolve tax profile.", nameof(jurisdiction));
        }

        var product = await _productRepository.GetByIdAsync(new ProductId(productId), cancellationToken);
        var productName = product?.Name.Value ?? productId.ToString();

        // 1. Check Product's explicit tax classification
        if (product != null)
        {
            var taxConfig = product.TaxConfiguration;

            if (taxConfig.TaxClassification == TaxClassification.Exempt)
            {
                return new TaxResolutionResult(
                    null,
                    taxConfig.TaxCode ?? "PK-EXEMPT",
                    "Sales Tax (Exempt)",
                    taxConfig.Authority ?? "FBR",
                    jurisdiction,
                    TaxClassification.Exempt,
                    DetermineItemClassification(itemType),
                    0.00m,
                    false,
                    null,
                    "Sixth Schedule / Statutory Exemption");
            }

            if (taxConfig.TaxClassification == TaxClassification.ZeroRated)
            {
                return new TaxResolutionResult(
                    null,
                    taxConfig.TaxCode ?? "PK-ZERO",
                    "Sales Tax (Zero-Rated)",
                    taxConfig.Authority ?? "FBR",
                    jurisdiction,
                    TaxClassification.ZeroRated,
                    DetermineItemClassification(itemType),
                    0.00m,
                    false,
                    null,
                    "Fifth Schedule / Zero-Rated");
            }

            if (taxConfig.TaxClassification == TaxClassification.OutOfScope)
            {
                return new TaxResolutionResult(
                    null,
                    taxConfig.TaxCode ?? "PK-OUT-OF-SCOPE",
                    "Non-Taxable",
                    taxConfig.Authority ?? "N/A",
                    jurisdiction,
                    TaxClassification.OutOfScope,
                    DetermineItemClassification(itemType),
                    0.00m,
                    false,
                    null,
                    "Out of Scope");
            }

            // Precedence 1: Product has an assigned TaxProfileId
            if (taxConfig.TaxProfileId.HasValue)
            {
                var profile = await _taxProfileRepository.GetByIdAsync(taxConfig.TaxProfileId.Value, cancellationToken);
                if (profile != null && profile.IsEffectiveAt(effectiveAtUtc))
                {
                    return MapProfileToResult(profile, tenderType, isPaymentTenderVerified);
                }
            }

            // Legacy direct rate fallback on Product if non-zero rate was configured directly
            if (taxConfig.RatePercentage > 0m && !taxConfig.TaxProfileId.HasValue)
            {
                return new TaxResolutionResult(
                    null,
                    taxConfig.TaxCode ?? $"PK-{jurisdiction.ToUpperInvariant()}-{taxConfig.RatePercentage:0}",
                    $"Sales Tax ({taxConfig.RatePercentage:0.##}%)",
                    taxConfig.Authority ?? "Tax Authority",
                    jurisdiction,
                    TaxClassification.Taxable,
                    DetermineItemClassification(itemType),
                    taxConfig.RatePercentage,
                    taxConfig.IsInclusive,
                    null,
                    "Product-Level Configuration");
            }
        }

        // Precedence 2: Category default tax profile (deferred to CBOS 1.3.0 schema migration)
        // ProductCategory does not currently persist DefaultTaxProfileId on the frozen 1.2.3 baseline.

        // Precedence 3: Jurisdiction and item classification default profile
        var itemClassification = DetermineItemClassification(itemType);
        var effectiveProfiles = await _taxProfileRepository.GetEffectiveAsync(effectiveAtUtc, cancellationToken);

        var matchingProfiles = effectiveProfiles
            .Where(p => string.Equals(p.Jurisdiction, jurisdiction, StringComparison.OrdinalIgnoreCase) &&
                        p.ItemClassification == itemClassification &&
                        p.TaxClassification == TaxClassification.Taxable)
            .ToList();

        if (matchingProfiles.Count > 1)
        {
            // If one has a payment restriction and one doesn't, filter by restriction
            var standardProfiles = matchingProfiles.Where(p => string.IsNullOrEmpty(p.PaymentMethodRestriction)).ToList();
            if (standardProfiles.Count > 1)
            {
                throw CatalogDomainException.ConflictingTaxProfile(
                    string.Join(", ", standardProfiles.Select(p => p.Code)),
                    jurisdiction);
            }

            var chosen = EvaluateTenderProfile(matchingProfiles, tenderType, isPaymentTenderVerified);
            if (chosen != null)
            {
                return MapProfileToResult(chosen, tenderType, isPaymentTenderVerified);
            }
        }
        else if (matchingProfiles.Count == 1)
        {
            return MapProfileToResult(matchingProfiles[0], tenderType, isPaymentTenderVerified);
        }

        // Prohibit silent zero fallback for taxable items
        throw CatalogDomainException.UnresolvedTaxConfiguration(productName, jurisdiction);
    }

    private static ItemTaxClassification DetermineItemClassification(string itemType)
    {
        // In Pakistan tax law, restaurant/prepared food and beverage service is classified as Services (provincial).
        // Packaged resale goods, retail merchandise are Goods (federal FBR).
        if (string.Equals(itemType, "Service", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(itemType, "Prepared", StringComparison.OrdinalIgnoreCase))
        {
            return ItemTaxClassification.Services;
        }

        return ItemTaxClassification.Goods;
    }

    private static TaxResolutionResult MapProfileToResult(
        TaxProfile profile,
        string? tenderType,
        bool isPaymentTenderVerified)
    {
        decimal rate = profile.RatePercentage;
        string code = profile.Code;
        string displayName = profile.InvoiceDisplayName;

        // Check if tender qualification applies
        if (!string.IsNullOrEmpty(profile.PaymentMethodRestriction) &&
            string.Equals(profile.PaymentMethodRestriction, "DigitalPaymentOnly", StringComparison.OrdinalIgnoreCase))
        {
            // Concessionary rate applies only if digital payment tender is verified
            bool qualifies = isPaymentTenderVerified &&
                             tenderType != null &&
                             (tenderType.Contains("card", StringComparison.OrdinalIgnoreCase) ||
                              tenderType.Contains("digital", StringComparison.OrdinalIgnoreCase));

            if (!qualifies)
            {
                // Concession does not apply
                rate = profile.RatePercentage;
            }
        }

        return new TaxResolutionResult(
            profile.Id,
            code,
            displayName,
            profile.Authority,
            profile.Jurisdiction,
            profile.TaxClassification,
            profile.ItemClassification,
            rate,
            profile.IsInclusive,
            profile.PaymentMethodRestriction,
            profile.ReferenceDocument);
    }

    private static TaxProfile? EvaluateTenderProfile(
        List<TaxProfile> profiles,
        string? tenderType,
        bool isPaymentTenderVerified)
    {
        if (isPaymentTenderVerified &&
            tenderType != null &&
            (tenderType.Contains("card", StringComparison.OrdinalIgnoreCase) ||
             tenderType.Contains("digital", StringComparison.OrdinalIgnoreCase)))
        {
            var concessionProfile = profiles.FirstOrDefault(p =>
                string.Equals(p.PaymentMethodRestriction, "DigitalPaymentOnly", StringComparison.OrdinalIgnoreCase));
            if (concessionProfile != null) return concessionProfile;
        }

        return profiles.FirstOrDefault(p => string.IsNullOrEmpty(p.PaymentMethodRestriction))
               ?? profiles.FirstOrDefault();
    }
}
