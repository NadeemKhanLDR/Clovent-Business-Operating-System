using Clovent.Domain;

namespace Clovent.Catalog.TaxProfiles;

/// <summary>
/// Configurable Pakistan sales tax profile aggregate.
/// Governs tax authority, provincial/federal jurisdiction, tax classification (Taxable, Zero-Rated,
/// Exempt, Out of Scope), item classification (Goods vs Services), rate, and pricing mode.
/// Supports effective date windows, payment-method concessions, and configuration audit history.
/// </summary>
public sealed class TaxProfile : AggregateRoot<TaxProfileId>
{
    /// <summary>Unique statutory code identifying the profile (e.g. "PK-PRA-SRV-16", "PK-FBR-GDS-18", "PK-EXEMPT").</summary>
    public string Code { get; private set; }

    /// <summary>Human-readable name printed on customer bills/invoices (e.g. "Punjab Sales Tax (Services)").</summary>
    public string InvoiceDisplayName { get; private set; }

    /// <summary>Tax revenue authority governing this tax (e.g. "PRA", "SRB", "KPRA", "BRA", "ICTRA", "FBR").</summary>
    public string Authority { get; private set; }

    /// <summary>Geographic/political jurisdiction (e.g. "Punjab", "Sindh", "Federal", "KP", "Balochistan", "Islamabad").</summary>
    public string Jurisdiction { get; private set; }

    /// <summary>Statutory taxability classification (Taxable, ZeroRated, Exempt, OutOfScope).</summary>
    public TaxClassification TaxClassification { get; private set; }

    /// <summary>Goods vs Services classification under constitutional sales tax separation.</summary>
    public ItemTaxClassification ItemClassification { get; private set; }

    /// <summary>Tax rate percentage (e.g. 16.00m for 16%, 0.00m for exempt/zero-rated).</summary>
    public decimal RatePercentage { get; private set; }

    /// <summary>Whether catalog prices include tax (Inclusive) or exclude tax (Exclusive).</summary>
    public TaxPricingMode PricingMode { get; private set; }

    /// <summary>Convenience helper for inclusive pricing check.</summary>
    public bool IsInclusive => PricingMode == TaxPricingMode.Inclusive;

    /// <summary>Instant from which this tax profile rate becomes legally effective.</summary>
    public DateTimeOffset EffectiveFromUtc { get; private set; }

    /// <summary>Optional instant when this profile expires or is superseded.</summary>
    public DateTimeOffset? EffectiveToUtc { get; private set; }

    /// <summary>Operational status flag.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Official statutory instrument or gazette notification reference (e.g. "PSTSA 2012 / Notification No. PRA/2024").</summary>
    public string? ReferenceDocument { get; private set; }

    /// <summary>Optional tender restriction if this rate is conditional (e.g. "DigitalPaymentOnly" for 5% card POS rate).</summary>
    public string? PaymentMethodRestriction { get; private set; }

    /// <summary>Audit/configuration notes.</summary>
    public string? Notes { get; private set; }

    /// <summary>UTC instant this profile was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>UTC instant this profile was last modified.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Constructor for EF Core persistence.</summary>
    private TaxProfile(
        TaxProfileId id,
        string code,
        string invoiceDisplayName,
        string authority,
        string jurisdiction,
        TaxClassification taxClassification,
        ItemTaxClassification itemClassification,
        decimal ratePercentage,
        TaxPricingMode pricingMode,
        DateTimeOffset effectiveFromUtc,
        DateTimeOffset? effectiveToUtc,
        bool isActive,
        string? referenceDocument,
        string? paymentMethodRestriction,
        string? notes,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        Code = code;
        InvoiceDisplayName = invoiceDisplayName;
        Authority = authority;
        Jurisdiction = jurisdiction;
        TaxClassification = taxClassification;
        ItemClassification = itemClassification;
        RatePercentage = ratePercentage;
        PricingMode = pricingMode;
        EffectiveFromUtc = effectiveFromUtc;
        EffectiveToUtc = effectiveToUtc;
        IsActive = isActive;
        ReferenceDocument = referenceDocument;
        PaymentMethodRestriction = paymentMethodRestriction;
        Notes = notes;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>Creates a new TaxProfile.</summary>
    public static TaxProfile Create(
        string code,
        string invoiceDisplayName,
        string authority,
        string jurisdiction,
        TaxClassification taxClassification,
        ItemTaxClassification itemClassification,
        decimal ratePercentage,
        TaxPricingMode pricingMode,
        DateTimeOffset effectiveFromUtc,
        DateTimeOffset? effectiveToUtc = null,
        string? referenceDocument = null,
        string? paymentMethodRestriction = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Tax profile code cannot be empty.", nameof(code));
        if (string.IsNullOrWhiteSpace(invoiceDisplayName))
            throw new ArgumentException("Invoice display name cannot be empty.", nameof(invoiceDisplayName));
        if (string.IsNullOrWhiteSpace(authority))
            throw new ArgumentException("Authority cannot be empty.", nameof(authority));
        if (string.IsNullOrWhiteSpace(jurisdiction))
            throw new ArgumentException("Jurisdiction cannot be empty.", nameof(jurisdiction));
        if (ratePercentage < 0 || ratePercentage > 100)
            throw new ArgumentOutOfRangeException(nameof(ratePercentage), ratePercentage, "Tax rate must be between 0 and 100.");

        if (taxClassification != TaxClassification.Taxable && ratePercentage != 0m)
            throw new ArgumentException("Non-taxable, zero-rated, and exempt profiles must have a 0% tax rate.", nameof(ratePercentage));

        var now = DateTimeOffset.UtcNow;
        return new TaxProfile(
            TaxProfileId.New(),
            code.Trim().ToUpperInvariant(),
            invoiceDisplayName.Trim(),
            authority.Trim().ToUpperInvariant(),
            jurisdiction.Trim(),
            taxClassification,
            itemClassification,
            ratePercentage,
            pricingMode,
            effectiveFromUtc,
            effectiveToUtc,
            true,
            referenceDocument?.Trim(),
            paymentMethodRestriction?.Trim(),
            notes?.Trim(),
            now,
            now);
    }

    /// <summary>Checks whether this profile is currently effective at a specified UTC instant.</summary>
    public bool IsEffectiveAt(DateTimeOffset instant)
    {
        if (!IsActive) return false;
        if (instant < EffectiveFromUtc) return false;
        if (EffectiveToUtc.HasValue && instant > EffectiveToUtc.Value) return false;
        return true;
    }

    /// <summary>Updates rate and pricing mode with an audit timestamp.</summary>
    public void UpdateRate(decimal newRate, TaxPricingMode newPricingMode, string? referenceDocument = null)
    {
        if (newRate < 0 || newRate > 100)
            throw new ArgumentOutOfRangeException(nameof(newRate), newRate, "Tax rate must be between 0 and 100.");
        if (TaxClassification != TaxClassification.Taxable && newRate != 0m)
            throw new ArgumentException("Non-taxable profiles cannot have a non-zero rate.", nameof(newRate));

        RatePercentage = newRate;
        PricingMode = newPricingMode;
        if (!string.IsNullOrWhiteSpace(referenceDocument))
        {
            ReferenceDocument = referenceDocument.Trim();
        }
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Deactivates the tax profile.</summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Activates the tax profile.</summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
