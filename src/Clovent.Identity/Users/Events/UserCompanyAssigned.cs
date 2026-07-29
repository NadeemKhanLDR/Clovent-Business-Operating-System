using Clovent.Domain;
using Clovent.Identity.Companies;

namespace Clovent.Identity.Users.Events;

/// <summary>Raised when a <see cref="User"/> is assigned to a <see cref="Company"/>.</summary>
public sealed record UserCompanyAssigned(UserId UserId, CompanyId CompanyId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
