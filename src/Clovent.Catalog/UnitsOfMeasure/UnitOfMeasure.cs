using Clovent.Catalog.Shared;
using Clovent.Catalog.UnitsOfMeasure.Events;
using Clovent.Catalog.UnitsOfMeasure.ValueObjects;
using Clovent.Domain;

namespace Clovent.Catalog.UnitsOfMeasure;

/// <summary>A unit of measure catalog entry (e.g. Kilogram, Piece, Box) - reference data shared across the catalog, referenced by <see cref="Products.Product"/>'s base unit and each <see cref="Variants.ProductVariant"/>'s own unit.</summary>
public sealed class UnitOfMeasure : AggregateRoot<UnitOfMeasureId>
{
    private const int MaxNameLength = 100;

    /// <summary>The short code (e.g. "KG").</summary>
    public UnitOfMeasureCode Code { get; }

    /// <summary>The display name (e.g. "Kilogram").</summary>
    public string Name { get; private set; }

    /// <summary>The unit's current lifecycle state.</summary>
    public CatalogStatus Status { get; private set; }

    /// <summary>UTC instant this unit was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private UnitOfMeasure(UnitOfMeasureId id, UnitOfMeasureCode code, string name, CatalogStatus status, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Code = code;
        Name = name;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new, active unit of measure catalog entry.</summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or too long.</exception>
    public static UnitOfMeasure Create(UnitOfMeasureCode code, string name)
    {
        ArgumentNullException.ThrowIfNull(code);
        name = RequireName(name);

        var now = DateTimeOffset.UtcNow;
        var unit = new UnitOfMeasure(UnitOfMeasureId.New(), code, name, CatalogStatus.Active, now);
        unit.AddDomainEvent(new UnitOfMeasureCreated(unit.Id, unit.Code, now));
        return unit;
    }

    /// <summary>Renames the unit. A no-op (no event raised) if unchanged.</summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or too long.</exception>
    public void Rename(string name)
    {
        name = RequireName(name);
        if (Name == name) return;

        Name = name;
        AddDomainEvent(new UnitOfMeasureRenamed(Id, name, DateTimeOffset.UtcNow));
    }

    /// <summary>Activates the unit.</summary>
    /// <exception cref="CatalogDomainException">The unit is already active.</exception>
    public void Activate()
    {
        if (Status == CatalogStatus.Active)
            throw CatalogDomainException.UnitOfMeasureAlreadyActive(Id);

        Status = CatalogStatus.Active;
        AddDomainEvent(new UnitOfMeasureActivated(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Deactivates the unit.</summary>
    /// <exception cref="CatalogDomainException">The unit is not active.</exception>
    public void Deactivate()
    {
        if (Status != CatalogStatus.Active)
            throw CatalogDomainException.UnitOfMeasureNotActive(Id);

        Status = CatalogStatus.Inactive;
        AddDomainEvent(new UnitOfMeasureDeactivated(Id, DateTimeOffset.UtcNow));
    }

    private static string RequireName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Unit of measure name is required.", nameof(value));

        value = value.Trim();

        if (value.Length > MaxNameLength)
            throw new ArgumentException($"Unit of measure name cannot exceed {MaxNameLength} characters.", nameof(value));

        return value;
    }
}
