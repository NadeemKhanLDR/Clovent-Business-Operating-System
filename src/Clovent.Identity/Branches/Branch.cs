using Clovent.Domain;
using Clovent.Identity.Branches.Events;
using Clovent.Identity.Branches.ValueObjects;
using Clovent.Identity.Companies;
using Clovent.Identity.Shared.ValueObjects;

namespace Clovent.Identity.Branches;

/// <summary>
/// A physical or logical location belonging to exactly one <see cref="Company"/>.
/// Milestone 13 added <see cref="Status"/> and <see cref="Address"/> - see
/// <see cref="Organizations.Organization"/>'s identical doc comment for the reasoning.
/// </summary>
public sealed class Branch : AggregateRoot<BranchId>
{
    /// <summary>The company this branch belongs to, fixed at creation.</summary>
    public CompanyId CompanyId { get; }

    /// <summary>The branch's display name.</summary>
    public BranchName Name { get; private set; }

    /// <summary>The branch's physical address, if recorded.</summary>
    public Address? Address { get; private set; }

    /// <summary>The branch's current lifecycle state.</summary>
    public BranchStatus Status { get; private set; }

    /// <summary>UTC instant this branch was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly - see <see cref="Organizations.Organization"/>'s identical constructor doc comment for why.</summary>
    private Branch(BranchId id, CompanyId companyId, BranchName name, Address? address, BranchStatus status, DateTimeOffset createdAtUtc)
    {
        Id = id;
        CompanyId = companyId;
        Name = name;
        Address = address;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new, active branch under the given company.</summary>
    public static Branch Create(CompanyId companyId, BranchName name, Address? address = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        var now = DateTimeOffset.UtcNow;
        var branch = new Branch(BranchId.New(), companyId, name, address, BranchStatus.Active, now);
        branch.AddDomainEvent(new BranchCreated(branch.Id, branch.CompanyId, branch.Name, now));
        return branch;
    }

    /// <summary>Renames the branch. A no-op (no event raised) if unchanged.</summary>
    public void Rename(BranchName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (Name == name) return;

        Name = name;
        AddDomainEvent(new BranchRenamed(Id, name, DateTimeOffset.UtcNow));
    }

    /// <summary>Sets or clears the branch's address.</summary>
    public void SetAddress(Address? address)
    {
        Address = address;
        if (address is not null)
        {
            AddDomainEvent(new BranchAddressChanged(Id, address, DateTimeOffset.UtcNow));
        }
    }

    /// <summary>Activates the branch.</summary>
    /// <exception cref="IdentityDomainException">The branch is already active.</exception>
    public void Activate()
    {
        if (Status == BranchStatus.Active)
            throw IdentityDomainException.BranchAlreadyActive(Id);

        Status = BranchStatus.Active;
        AddDomainEvent(new BranchActivated(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Deactivates the branch.</summary>
    /// <exception cref="IdentityDomainException">The branch is not active.</exception>
    public void Deactivate()
    {
        if (Status != BranchStatus.Active)
            throw IdentityDomainException.BranchNotActive(Id);

        Status = BranchStatus.Inactive;
        AddDomainEvent(new BranchDeactivated(Id, DateTimeOffset.UtcNow));
    }
}
