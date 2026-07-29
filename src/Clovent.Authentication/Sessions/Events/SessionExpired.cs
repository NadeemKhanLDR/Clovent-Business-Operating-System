using Clovent.Domain;

namespace Clovent.Authentication.Sessions.Events;

/// <summary>Raised when a <see cref="Session"/> transitions to expired because its idle timeout elapsed.</summary>
public sealed record SessionExpired(SessionId SessionId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
