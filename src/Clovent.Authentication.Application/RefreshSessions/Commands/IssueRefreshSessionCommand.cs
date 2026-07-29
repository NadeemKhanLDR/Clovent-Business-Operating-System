using Clovent.Authentication.Application.RefreshSessions.Dtos;
using Clovent.Authentication.RefreshSessions;
using Clovent.Authentication.Sessions;
using MediatR;

namespace Clovent.Authentication.Application.RefreshSessions.Commands;

/// <summary>Issues a new refresh session for an active session.</summary>
public sealed record IssueRefreshSessionCommand(Guid SessionId, TimeSpan? Lifetime = null) : IRequest<RefreshSessionDto>;

/// <summary>Handles <see cref="IssueRefreshSessionCommand"/>.</summary>
public sealed class IssueRefreshSessionCommandHandler(
    IRefreshSessionRepository refreshSessionRepository,
    ISessionRepository sessionRepository,
    TimeProvider timeProvider) : IRequestHandler<IssueRefreshSessionCommand, RefreshSessionDto>
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(7);

    /// <inheritdoc/>
    public async Task<RefreshSessionDto> Handle(IssueRefreshSessionCommand request, CancellationToken cancellationToken)
    {
        var sessionId = new SessionId(request.SessionId);
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Session), sessionId);

        var refreshSession = RefreshSession.Issue(session.Id, request.Lifetime ?? DefaultLifetime, timeProvider.GetUtcNow());
        await refreshSessionRepository.AddAsync(refreshSession, cancellationToken);

        return RefreshSessionDto.FromDomain(refreshSession);
    }
}
