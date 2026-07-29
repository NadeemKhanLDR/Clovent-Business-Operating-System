using Clovent.Authentication.RefreshSessions;

namespace Clovent.Authentication.Application.RefreshSessions.Dtos;

/// <summary>Read-model shape for a <see cref="RefreshSession"/>, safe to cross a process boundary.</summary>
public sealed record RefreshSessionDto(
    Guid RefreshSessionId,
    Guid SessionId,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string Status)
{
    /// <summary>Projects a domain <see cref="RefreshSession"/> into its DTO.</summary>
    public static RefreshSessionDto FromDomain(RefreshSession refreshSession) => new(
        refreshSession.Id.Value,
        refreshSession.SessionId.Value,
        refreshSession.IssuedAtUtc,
        refreshSession.ExpiresAtUtc,
        refreshSession.Status.ToString());
}
