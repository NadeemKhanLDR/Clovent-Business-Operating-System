using Clovent.Domain;

namespace Clovent.MasterData.Terminals.Events;

/// <summary>Raised when a <see cref="Terminal"/> is deactivated.</summary>
public sealed record TerminalDeactivated(TerminalId TerminalId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
