using Clovent.Domain;
using Clovent.MasterData.FiscalYears.ValueObjects;

namespace Clovent.MasterData.FiscalYears.Events;

/// <summary>Raised when a <see cref="FiscalYear"/>'s name changes.</summary>
public sealed record FiscalYearRenamed(FiscalYearId FiscalYearId, FiscalYearName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
