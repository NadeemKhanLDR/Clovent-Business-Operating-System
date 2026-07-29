using Clovent.Domain;

namespace Clovent.MasterData.Departments.Events;

/// <summary>Raised when a <see cref="Department"/> is deactivated.</summary>
public sealed record DepartmentDeactivated(DepartmentId DepartmentId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
