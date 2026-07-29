using Clovent.Catalog.Barcodes.Events;
using Clovent.Catalog.Barcodes.ValueObjects;
using Clovent.Catalog.Shared;
using Clovent.Catalog.Variants;
using Clovent.Domain;

namespace Clovent.Catalog.Barcodes;

/// <summary>
/// A scannable code identifying one <see cref="ProductVariant"/>. A variant
/// may have several (e.g. a case barcode and an each barcode), with at most
/// one marked <see cref="IsPrimary"/> - enforcing "at most one primary
/// across a variant's barcodes" is an Application-layer concern (this
/// aggregate has no visibility into its siblings), the same
/// "cross-aggregate consistency is the handler's job" pattern already
/// established for <c>Organization.AddCompany</c>. Uniqueness of
/// <see cref="Value"/> across the whole catalog is enforced at the
/// Infrastructure layer (a unique index), not here.
/// </summary>
public sealed class Barcode : AggregateRoot<BarcodeId>
{
    /// <summary>The variant this barcode identifies, fixed at creation.</summary>
    public ProductVariantId ProductVariantId { get; }

    /// <summary>The scanned code, fixed at creation.</summary>
    public BarcodeValue Value { get; }

    /// <summary>Whether this is the variant's primary (default-scanned) barcode.</summary>
    public bool IsPrimary { get; private set; }

    /// <summary>The barcode's current lifecycle state.</summary>
    public CatalogStatus Status { get; private set; }

    /// <summary>UTC instant this barcode was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private Barcode(BarcodeId id, ProductVariantId productVariantId, BarcodeValue value, bool isPrimary, CatalogStatus status, DateTimeOffset createdAtUtc)
    {
        Id = id;
        ProductVariantId = productVariantId;
        Value = value;
        IsPrimary = isPrimary;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new, active barcode for the given variant.</summary>
    public static Barcode Create(ProductVariantId productVariantId, BarcodeValue value, bool isPrimary = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var now = DateTimeOffset.UtcNow;
        var barcode = new Barcode(BarcodeId.New(), productVariantId, value, isPrimary, CatalogStatus.Active, now);
        barcode.AddDomainEvent(new BarcodeCreated(barcode.Id, barcode.ProductVariantId, barcode.Value, now));
        return barcode;
    }

    /// <summary>Marks this barcode as the variant's primary one.</summary>
    public void MarkAsPrimary()
    {
        if (IsPrimary) return;

        IsPrimary = true;
        AddDomainEvent(new BarcodePrimaryChanged(Id, true, DateTimeOffset.UtcNow));
    }

    /// <summary>Unmarks this barcode as the variant's primary one.</summary>
    public void UnmarkAsPrimary()
    {
        if (!IsPrimary) return;

        IsPrimary = false;
        AddDomainEvent(new BarcodePrimaryChanged(Id, false, DateTimeOffset.UtcNow));
    }

    /// <summary>Activates the barcode.</summary>
    /// <exception cref="CatalogDomainException">The barcode is already active.</exception>
    public void Activate()
    {
        if (Status == CatalogStatus.Active)
            throw CatalogDomainException.BarcodeAlreadyActive(Id);

        Status = CatalogStatus.Active;
        AddDomainEvent(new BarcodeActivated(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Deactivates the barcode.</summary>
    /// <exception cref="CatalogDomainException">The barcode is not active.</exception>
    public void Deactivate()
    {
        if (Status != CatalogStatus.Active)
            throw CatalogDomainException.BarcodeNotActive(Id);

        Status = CatalogStatus.Inactive;
        AddDomainEvent(new BarcodeDeactivated(Id, DateTimeOffset.UtcNow));
    }
}
