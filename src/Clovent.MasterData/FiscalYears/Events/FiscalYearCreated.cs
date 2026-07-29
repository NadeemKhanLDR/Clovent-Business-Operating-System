using Clovent.Domain;
using Clovent.Identity.Organizations;
using Clovent.MasterData.FiscalYears.ValueObjects;

namespace Clovent.MasterData.FiscalYears.Events;

/// <summary>Raised when a new <see cref="FiscalYear"/> is created.</summary>
public sealed record FiscalYearCreated(FiscalYearId FiscalYearId, OrganizationId OrganizationId, FiscalYearName Name, DateOnly StartDate, DateOnly EndDate, DateTimeOffset OccurredOnUtc) : IDomainEvent;
