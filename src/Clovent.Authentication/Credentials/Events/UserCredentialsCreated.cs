using Clovent.Domain;
using Clovent.Identity.Users;

namespace Clovent.Authentication.Credentials.Events;

/// <summary>Raised when a new <see cref="UserCredentials"/> record is created for a user.</summary>
public sealed record UserCredentialsCreated(UserCredentialsId UserCredentialsId, UserId UserId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
