using Clovent.Domain;
using Clovent.Identity.Branches;
using Clovent.MasterData.Departments.ValueObjects;

namespace Clovent.MasterData.Departments.Events;

/// <summary>Raised when a new <see cref="Department"/> is created.</summary>
public sealed record DepartmentCreated(DepartmentId DepartmentId, BranchId BranchId, DepartmentName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
