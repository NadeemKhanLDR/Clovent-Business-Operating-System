using Clovent.Domain;

namespace Clovent.MasterData.FiscalYears.Events;

/// <summary>Raised when a <see cref="FiscalYear"/> is closed.</summary>
public sealed record FiscalYearClosed(FiscalYearId FiscalYearId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
