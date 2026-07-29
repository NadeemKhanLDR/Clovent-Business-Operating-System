using Clovent.Domain;
using Clovent.Identity.Users;

namespace Clovent.Authentication.Credentials.Events;

/// <summary>Raised when a user's <see cref="PasswordHash"/> is changed.</summary>
public sealed record PasswordChanged(UserCredentialsId UserCredentialsId, UserId UserId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
