using Clovent.Authentication.Sessions;
using MediatR;

namespace Clovent.Authentication.Application.Sessions.Commands;

/// <summary>Transitions a session to expired because its idle timeout elapsed, and invalidates its active refresh session, if any.</summary>
public sealed record ExpireSessionCommand(Guid SessionId) : IRequest;

/// <summary>Handles <see cref="ExpireSessionCommand"/>.</summary>
public sealed class ExpireSessionCommandHandler(
    ISessionRepository sessionRepository,
    SessionTerminationCascade cascade,
    TimeProvider timeProvider) : IRequestHandler<ExpireSessionCommand>
{
    /// <inheritdoc/>
    public async Task Handle(ExpireSessionCommand request, CancellationToken cancellationToken)
    {
        var sessionId = new SessionId(request.SessionId);
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Session), sessionId);

        var now = timeProvider.GetUtcNow();
        session.Expire(now);
        await cascade.ApplyAsync(sessionId, now, cancellationToken);
    }
}
