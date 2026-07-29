using Clovent.Domain;
using Clovent.Identity.Branches;
using Clovent.Identity.Companies.Events;
using Clovent.Identity.Companies.ValueObjects;
using Clovent.Identity.Organizations;
using Clovent.Identity.Shared.ValueObjects;

namespace Clovent.Identity.Companies;

/// <summary>
/// A legal entity belonging to exactly one <see cref="Organization"/>, owning
/// zero or more <see cref="Branch"/> aggregates by identity reference only -
/// Branch is its own aggregate root with its own repository. Milestone 13
/// added <see cref="Status"/> and <see cref="TaxId"/> - see <see cref="Organizations.Organization"/>'s
/// identical doc comment for the reasoning.
/// </summary>
public sealed class Company : AggregateRoot<CompanyId>
{
    private readonly HashSet<BranchId> _branchIds;

    /// <summary>The organization this company belongs to, fixed at creation.</summary>
    public OrganizationId OrganizationId { get; }

    /// <summary>The company's registered/trading name.</summary>
    public CompanyName Name { get; private set; }

    /// <summary>The company's tax registration identifier, if recorded.</summary>
    public TaxId? TaxId { get; private set; }

    /// <summary>The company's current lifecycle state.</summary>
    public CompanyStatus Status { get; private set; }

    /// <summary>The branches currently belonging to this company.</summary>
    public IReadOnlyCollection<BranchId> BranchIds => _branchIds;

    /// <summary>UTC instant this company was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly - see <see cref="Organizations.Organization"/>'s identical constructor doc comment for why.</summary>
    private Company(
        CompanyId id,
        OrganizationId organizationId,
        CompanyName name,
        TaxId? taxId,
        CompanyStatus status,
        DateTimeOffset createdAtUtc,
        IReadOnlyCollection<BranchId> branchIds)
    {
        Id = id;
        OrganizationId = organizationId;
        Name = name;
        TaxId = taxId;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        _branchIds = [.. branchIds];
    }

    /// <summary>Creates a new, active company under the given organization, with no branches.</summary>
    public static Company Create(OrganizationId organizationId, CompanyName name, TaxId? taxId = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        var now = DateTimeOffset.UtcNow;
        var company = new Company(CompanyId.New(), organizationId, name, taxId, CompanyStatus.Active, now, []);
        company.AddDomainEvent(new CompanyCreated(company.Id, company.OrganizationId, company.Name, now));
        return company;
    }

    /// <summary>Renames the company. A no-op (no event raised) if unchanged.</summary>
    public void Rename(CompanyName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (Name == name) return;

        Name = name;
        AddDomainEvent(new CompanyRenamed(Id, name, DateTimeOffset.UtcNow));
    }

    /// <summary>Sets or clears the company's tax id.</summary>
    public void SetTaxId(TaxId? taxId) => TaxId = taxId;

    /// <summary>Activates the company.</summary>
    /// <exception cref="IdentityDomainException">The company is already active.</exception>
    public void Activate()
    {
        if (Status == CompanyStatus.Active)
            throw IdentityDomainException.CompanyAlreadyActive(Id);

        Status = CompanyStatus.Active;
        AddDomainEvent(new CompanyActivated(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Deactivates the company.</summary>
    /// <exception cref="IdentityDomainException">The company is not active.</exception>
    public void Deactivate()
    {
        if (Status != CompanyStatus.Active)
            throw IdentityDomainException.CompanyNotActive(Id);

        Status = CompanyStatus.Inactive;
        AddDomainEvent(new CompanyDeactivated(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Records that a branch belongs to this company.</summary>
    /// <exception cref="IdentityDomainException">The branch already belongs to this company.</exception>
    public void AddBranch(BranchId branchId)
    {
        if (!_branchIds.Add(branchId))
            throw IdentityDomainException.BranchAlreadyBelongsToCompany(Id, branchId);

        AddDomainEvent(new BranchAddedToCompany(Id, branchId, DateTimeOffset.UtcNow));
    }

    /// <summary>Removes a branch from this company.</summary>
    /// <exception cref="IdentityDomainException">The branch does not belong to this company.</exception>
    public void RemoveBranch(BranchId branchId)
    {
        if (!_branchIds.Remove(branchId))
            throw IdentityDomainException.BranchDoesNotBelongToCompany(Id, branchId);

        AddDomainEvent(new BranchRemovedFromCompany(Id, branchId, DateTimeOffset.UtcNow));
    }
}
