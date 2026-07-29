using Clovent.Domain;

namespace Clovent.MasterData.Departments.Events;

/// <summary>Raised when a <see cref="Department"/> is (re)activated.</summary>
public sealed record DepartmentActivated(DepartmentId DepartmentId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
