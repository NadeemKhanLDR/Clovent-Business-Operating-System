using Clovent.Domain;
using Clovent.Identity.Companies;
using Clovent.Identity.Organizations.Events;
using Clovent.Identity.Organizations.ValueObjects;
using Clovent.Identity.Shared.ValueObjects;

namespace Clovent.Identity.Organizations;

/// <summary>
/// The top of the tenant hierarchy: an Organization owns zero or more
/// <see cref="Company"/> aggregates, referenced by identity only - Company
/// is its own aggregate root with its own repository, so Organization never
/// holds a full Company instance, only the fact that the two are related.
/// Milestone 13 ("Organization &amp; Master Data Foundation") added
/// <see cref="Status"/> and <see cref="TaxId"/> to what Milestone 4 first
/// modeled - additive, non-breaking changes; the membership-by-reference
/// design is unchanged.
/// </summary>
public sealed class Organization : AggregateRoot<OrganizationId>
{
    private readonly HashSet<CompanyId> _companyIds;

    /// <summary>The organization's registered/trading name.</summary>
    public OrganizationName Name { get; private set; }

    /// <summary>The organization's tax registration identifier, if recorded.</summary>
    public TaxId? TaxId { get; private set; }

    /// <summary>The organization's current lifecycle state.</summary>
    public OrganizationStatus Status { get; private set; }

    /// <summary>The companies currently belonging to this organization.</summary>
    public IReadOnlyCollection<CompanyId> CompanyIds => _companyIds;

    /// <summary>UTC instant this organization was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>
    /// Takes every persisted field explicitly so this is the single,
    /// unambiguous constructor an EF Core Infrastructure implementation can
    /// bind to when materializing an existing organization from storage -
    /// see the identical reasoning already applied to every other aggregate
    /// in this solution (<c>AuthenticationInfrastructure.md</c> Section 4).
    /// </summary>
    private Organization(
        OrganizationId id,
        OrganizationName name,
        TaxId? taxId,
        OrganizationStatus status,
        DateTimeOffset createdAtUtc,
        IReadOnlyCollection<CompanyId> companyIds)
    {
        Id = id;
        Name = name;
        TaxId = taxId;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        _companyIds = [.. companyIds];
    }

    /// <summary>Creates a new, active organization with no companies.</summary>
    public static Organization Create(OrganizationName name, TaxId? taxId = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        var now = DateTimeOffset.UtcNow;
        var organization = new Organization(OrganizationId.New(), name, taxId, OrganizationStatus.Active, now, []);
        organization.AddDomainEvent(new OrganizationCreated(organization.Id, organization.Name, now));
        return organization;
    }

    /// <summary>Renames the organization. A no-op (no event raised) if unchanged.</summary>
    public void Rename(OrganizationName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (Name == name) return;

        Name = name;
        AddDomainEvent(new OrganizationRenamed(Id, name, DateTimeOffset.UtcNow));
    }

    /// <summary>Sets or clears the organization's tax id.</summary>
    public void SetTaxId(TaxId? taxId) => TaxId = taxId;

    /// <summary>Activates the organization.</summary>
    /// <exception cref="IdentityDomainException">The organization is already active.</exception>
    public void Activate()
    {
        if (Status == OrganizationStatus.Active)
            throw IdentityDomainException.OrganizationAlreadyActive(Id);

        Status = OrganizationStatus.Active;
        AddDomainEvent(new OrganizationActivated(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Deactivates the organization.</summary>
    /// <exception cref="IdentityDomainException">The organization is not active.</exception>
    public void Deactivate()
    {
        if (Status != OrganizationStatus.Active)
            throw IdentityDomainException.OrganizationNotActive(Id);

        Status = OrganizationStatus.Inactive;
        AddDomainEvent(new OrganizationDeactivated(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Records that a company belongs to this organization.</summary>
    /// <exception cref="IdentityDomainException">The company already belongs to this organization.</exception>
    public void AddCompany(CompanyId companyId)
    {
        if (!_companyIds.Add(companyId))
            throw IdentityDomainException.CompanyAlreadyBelongsToOrganization(Id, companyId);

        AddDomainEvent(new CompanyAddedToOrganization(Id, companyId, DateTimeOffset.UtcNow));
    }

    /// <summary>Removes a company from this organization.</summary>
    /// <exception cref="IdentityDomainException">The company does not belong to this organization.</exception>
    public void RemoveCompany(CompanyId companyId)
    {
        if (!_companyIds.Remove(companyId))
            throw IdentityDomainException.CompanyDoesNotBelongToOrganization(Id, companyId);

        AddDomainEvent(new CompanyRemovedFromOrganization(Id, companyId, DateTimeOffset.UtcNow));
    }
}
