using Clovent.Authentication.LoginAttempts;

namespace Clovent.Authentication.Application.LoginAttempts.Dtos;

/// <summary>Read-model shape for a <see cref="LoginAttempt"/>, safe to cross a process boundary.</summary>
public sealed record LoginAttemptDto(
    Guid LoginAttemptId,
    string AttemptedIdentifier,
    Guid? UserId,
    string Outcome,
    DateTimeOffset OccurredAtUtc)
{
    /// <summary>Projects a domain <see cref="LoginAttempt"/> into its DTO.</summary>
    public static LoginAttemptDto FromDomain(LoginAttempt attempt) => new(
        attempt.Id.Value,
        attempt.AttemptedIdentifier,
        attempt.UserId?.Value,
        attempt.Outcome.ToString(),
        attempt.OccurredAtUtc);
}
