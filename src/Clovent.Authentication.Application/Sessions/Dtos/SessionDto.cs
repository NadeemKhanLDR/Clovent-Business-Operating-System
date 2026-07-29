using Clovent.Authentication.Sessions;

namespace Clovent.Authentication.Application.Sessions.Dtos;

/// <summary>Read-model shape for a <see cref="Session"/>, safe to cross a process boundary.</summary>
public sealed record SessionDto(
    Guid SessionId,
    Guid UserId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset LastActivityAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string Status)
{
    /// <summary>Projects a domain <see cref="Session"/> into its DTO.</summary>
    public static SessionDto FromDomain(Session session) => new(
        session.Id.Value,
        session.UserId.Value,
        session.StartedAtUtc,
        session.LastActivityAtUtc,
        session.ExpiresAtUtc,
        session.Status.ToString());
}
