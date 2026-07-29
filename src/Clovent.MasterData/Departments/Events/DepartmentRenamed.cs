using Clovent.Domain;
using Clovent.MasterData.Departments.ValueObjects;

namespace Clovent.MasterData.Departments.Events;

/// <summary>Raised when a <see cref="Department"/>'s name changes.</summary>
public sealed record DepartmentRenamed(DepartmentId DepartmentId, DepartmentName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
