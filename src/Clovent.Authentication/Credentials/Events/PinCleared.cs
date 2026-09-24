using Clovent.Domain;
using Clovent.Identity.Users;

namespace Clovent.Authentication.Credentials.Events;

/// <summary>Raised when a user's <see cref="UserCredentials.PinHash"/> is removed entirely.</summary>
public sealed record PinCleared(UserCredentialsId UserCredentialsId, UserId UserId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
