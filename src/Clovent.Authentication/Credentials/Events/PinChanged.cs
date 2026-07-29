using Clovent.Domain;
using Clovent.Identity.Users;

namespace Clovent.Authentication.Credentials.Events;

/// <summary>Raised when a user's <see cref="PinHash"/> is changed.</summary>
public sealed record PinChanged(UserCredentialsId UserCredentialsId, UserId UserId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
