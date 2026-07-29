using Clovent.Domain;

namespace Clovent.MasterData.Terminals.Events;

/// <summary>Raised when a <see cref="Terminal"/> is (re)activated.</summary>
public sealed record TerminalActivated(TerminalId TerminalId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
