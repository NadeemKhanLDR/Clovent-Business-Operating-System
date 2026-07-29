using Clovent.Authentication.RefreshSessions;
using MediatR;

namespace Clovent.Authentication.Application.RefreshSessions.Commands;

/// <summary>Administratively revokes an active refresh session.</summary>
public sealed record RevokeRefreshSessionCommand(Guid RefreshSessionId) : IRequest;

/// <summary>Handles <see cref="RevokeRefreshSessionCommand"/>.</summary>
public sealed class RevokeRefreshSessionCommandHandler(IRefreshSessionRepository refreshSessionRepository, TimeProvider timeProvider)
    : IRequestHandler<RevokeRefreshSessionCommand>
{
    /// <inheritdoc/>
    public async Task Handle(RevokeRefreshSessionCommand request, CancellationToken cancellationToken)
    {
        var refreshSessionId = new RefreshSessionId(request.RefreshSessionId);
        var refreshSession = await refreshSessionRepository.GetByIdAsync(refreshSessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(RefreshSession), refreshSessionId);

        refreshSession.Revoke(timeProvider.GetUtcNow());
    }
}
