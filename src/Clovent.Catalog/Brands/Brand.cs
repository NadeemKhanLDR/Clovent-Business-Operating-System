using Clovent.Catalog.Brands.Events;
using Clovent.Catalog.Brands.ValueObjects;
using Clovent.Catalog.Shared;
using Clovent.Domain;

namespace Clovent.Catalog.Brands;

/// <summary>A product brand (e.g. "Acme") - reference data shared across the catalog.</summary>
public sealed class Brand : AggregateRoot<BrandId>
{
    /// <summary>The brand's display name.</summary>
    public BrandName Name { get; private set; }

    /// <summary>The brand's current lifecycle state.</summary>
    public CatalogStatus Status { get; private set; }

    /// <summary>UTC instant this brand was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private Brand(BrandId id, BrandName name, CatalogStatus status, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Name = name;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new, active brand.</summary>
    public static Brand Create(BrandName name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var now = DateTimeOffset.UtcNow;
        var brand = new Brand(BrandId.New(), name, CatalogStatus.Active, now);
        brand.AddDomainEvent(new BrandCreated(brand.Id, brand.Name, now));
        return brand;
    }

    /// <summary>Renames the brand. A no-op (no event raised) if unchanged.</summary>
    public void Rename(BrandName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (Name == name) return;

        Name = name;
        AddDomainEvent(new BrandRenamed(Id, name, DateTimeOffset.UtcNow));
    }

    /// <summary>Activates the brand.</summary>
    /// <exception cref="CatalogDomainException">The brand is already active.</exception>
    public void Activate()
    {
        if (Status == CatalogStatus.Active)
            throw CatalogDomainException.BrandAlreadyActive(Id);

        Status = CatalogStatus.Active;
        AddDomainEvent(new BrandActivated(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Deactivates the brand.</summary>
    /// <exception cref="CatalogDomainException">The brand is not active.</exception>
    public void Deactivate()
    {
        if (Status != CatalogStatus.Active)
            throw CatalogDomainException.BrandNotActive(Id);

        Status = CatalogStatus.Inactive;
        AddDomainEvent(new BrandDeactivated(Id, DateTimeOffset.UtcNow));
    }
}
