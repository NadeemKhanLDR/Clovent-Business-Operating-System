using Clovent.Authentication.Sessions;
using MediatR;

namespace Clovent.Authentication.Application.Sessions.Commands;

/// <summary>Administratively revokes an active session and invalidates its active refresh session, if any.</summary>
public sealed record RevokeSessionCommand(Guid SessionId) : IRequest;

/// <summary>Handles <see cref="RevokeSessionCommand"/>.</summary>
public sealed class RevokeSessionCommandHandler(
    ISessionRepository sessionRepository,
    SessionTerminationCascade cascade,
    TimeProvider timeProvider) : IRequestHandler<RevokeSessionCommand>
{
    /// <inheritdoc/>
    public async Task Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
    {
        var sessionId = new SessionId(request.SessionId);
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Session), sessionId);

        var now = timeProvider.GetUtcNow();
        session.Revoke(now);
        await cascade.ApplyAsync(sessionId, now, cancellationToken);
    }
}
