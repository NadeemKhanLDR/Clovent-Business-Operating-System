using Clovent.Authentication.Sessions;
using MediatR;

namespace Clovent.Authentication.Application.Sessions.Commands;

/// <summary>Ends a session via the user's own explicit sign-out, and invalidates its active refresh session, if any.</summary>
public sealed record LogOutSessionCommand(Guid SessionId) : IRequest;

/// <summary>Handles <see cref="LogOutSessionCommand"/>.</summary>
public sealed class LogOutSessionCommandHandler(
    ISessionRepository sessionRepository,
    SessionTerminationCascade cascade,
    TimeProvider timeProvider) : IRequestHandler<LogOutSessionCommand>
{
    /// <inheritdoc/>
    public async Task Handle(LogOutSessionCommand request, CancellationToken cancellationToken)
    {
        var sessionId = new SessionId(request.SessionId);
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Session), sessionId);

        var now = timeProvider.GetUtcNow();
        session.LogOut(now);
        await cascade.ApplyAsync(sessionId, now, cancellationToken);
    }
}
