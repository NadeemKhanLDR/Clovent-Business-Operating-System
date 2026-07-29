using Clovent.Domain;
using Clovent.Identity.Branches;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Terminals.ValueObjects;

namespace Clovent.MasterData.Terminals.Events;

/// <summary>Raised when a new <see cref="Terminal"/> is created.</summary>
public sealed record TerminalCreated(TerminalId TerminalId, BranchId BranchId, TerminalName Name, EntityCode Code, DateTimeOffset OccurredOnUtc) : IDomainEvent;
