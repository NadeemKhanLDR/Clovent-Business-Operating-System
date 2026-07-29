using Clovent.Domain;
using Clovent.MasterData.Terminals.ValueObjects;

namespace Clovent.MasterData.Terminals.Events;

/// <summary>Raised when a <see cref="Terminal"/>'s name changes.</summary>
public sealed record TerminalRenamed(TerminalId TerminalId, TerminalName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
