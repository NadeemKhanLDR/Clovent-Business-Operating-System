using Clovent.Catalog.Groups.Events;
using Clovent.Catalog.Groups.ValueObjects;
using Clovent.Catalog.Shared;
using Clovent.Domain;

namespace Clovent.Catalog.Groups;

/// <summary>A flat (non-hierarchical) grouping for <see cref="Products.Product"/>s, distinct from <see cref="Categories.ProductCategory"/> - see <c>CatalogArchitecture.md</c> for why both exist.</summary>
public sealed class ProductGroup : AggregateRoot<ProductGroupId>
{
    /// <summary>The group's display name.</summary>
    public ProductGroupName Name { get; private set; }

    /// <summary>The group's current lifecycle state.</summary>
    public CatalogStatus Status { get; private set; }

    /// <summary>UTC instant this group was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private ProductGroup(ProductGroupId id, ProductGroupName name, CatalogStatus status, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Name = name;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new, active group.</summary>
    public static ProductGroup Create(ProductGroupName name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var now = DateTimeOffset.UtcNow;
        var group = new ProductGroup(ProductGroupId.New(), name, CatalogStatus.Active, now);
        group.AddDomainEvent(new ProductGroupCreated(group.Id, group.Name, now));
        return group;
    }

    /// <summary>Renames the group. A no-op (no event raised) if unchanged.</summary>
    public void Rename(ProductGroupName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (Name == name) return;

        Name = name;
        AddDomainEvent(new ProductGroupRenamed(Id, name, DateTimeOffset.UtcNow));
    }

    /// <summary>Activates the group.</summary>
    /// <exception cref="CatalogDomainException">The group is already active.</exception>
    public void Activate()
    {
        if (Status == CatalogStatus.Active)
            throw CatalogDomainException.GroupAlreadyActive(Id);

        Status = CatalogStatus.Active;
        AddDomainEvent(new ProductGroupActivated(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Deactivates the group.</summary>
    /// <exception cref="CatalogDomainException">The group is not active.</exception>
    public void Deactivate()
    {
        if (Status != CatalogStatus.Active)
            throw CatalogDomainException.GroupNotActive(Id);

        Status = CatalogStatus.Inactive;
        AddDomainEvent(new ProductGroupDeactivated(Id, DateTimeOffset.UtcNow));
    }
}
