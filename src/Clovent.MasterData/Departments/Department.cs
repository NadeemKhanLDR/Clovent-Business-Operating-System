using Clovent.Domain;
using Clovent.Identity.Branches;
using Clovent.MasterData.Departments.Events;
using Clovent.MasterData.Departments.ValueObjects;
using Clovent.MasterData.Shared;

namespace Clovent.MasterData.Departments;

/// <summary>
/// An organizational subdivision within a <see cref="Branch"/> (e.g.
/// "Kitchen", "Accounting"). References its owning branch by strongly-typed
/// id only - <see cref="Branch"/> is <c>Clovent.Identity</c>'s aggregate,
/// and this bounded context depends on that id the same way
/// <c>Clovent.Authentication</c> depends on <c>Clovent.Identity.Users.UserId</c>,
/// never loading or mutating the branch itself.
/// </summary>
public sealed class Department : AggregateRoot<DepartmentId>
{
    /// <summary>The branch this department belongs to, fixed at creation.</summary>
    public BranchId BranchId { get; }

    /// <summary>The department's display name.</summary>
    public DepartmentName Name { get; private set; }

    /// <summary>The department's current lifecycle state.</summary>
    public MasterDataStatus Status { get; private set; }

    /// <summary>UTC instant this department was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private Department(DepartmentId id, BranchId branchId, DepartmentName name, MasterDataStatus status, DateTimeOffset createdAtUtc)
    {
        Id = id;
        BranchId = branchId;
        Name = name;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new, active department under the given branch.</summary>
    public static Department Create(BranchId branchId, DepartmentName name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var now = DateTimeOffset.UtcNow;
        var department = new Department(DepartmentId.New(), branchId, name, MasterDataStatus.Active, now);
        department.AddDomainEvent(new DepartmentCreated(department.Id, department.BranchId, department.Name, now));
        return department;
    }

    /// <summary>Renames the department. A no-op (no event raised) if unchanged.</summary>
    public void Rename(DepartmentName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (Name == name) return;

        Name = name;
        AddDomainEvent(new DepartmentRenamed(Id, name, DateTimeOffset.UtcNow));
    }

    /// <summary>Activates the department.</summary>
    /// <exception cref="MasterDataDomainException">The department is already active.</exception>
    public void Activate()
    {
        if (Status == MasterDataStatus.Active)
            throw MasterDataDomainException.DepartmentAlreadyActive(Id);

        Status = MasterDataStatus.Active;
        AddDomainEvent(new DepartmentActivated(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Deactivates the department.</summary>
    /// <exception cref="MasterDataDomainException">The department is not active.</exception>
    public void Deactivate()
    {
        if (Status != MasterDataStatus.Active)
            throw MasterDataDomainException.DepartmentNotActive(Id);

        Status = MasterDataStatus.Inactive;
        AddDomainEvent(new DepartmentDeactivated(Id, DateTimeOffset.UtcNow));
    }
}
