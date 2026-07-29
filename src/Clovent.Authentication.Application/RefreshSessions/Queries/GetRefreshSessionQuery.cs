using Clovent.Authentication.Application.RefreshSessions.Dtos;
using Clovent.Authentication.RefreshSessions;
using MediatR;

namespace Clovent.Authentication.Application.RefreshSessions.Queries;

/// <summary>Retrieves a refresh session by identity, or <see langword="null"/> if none exists.</summary>
public sealed record GetRefreshSessionQuery(Guid RefreshSessionId) : IRequest<RefreshSessionDto?>;

/// <summary>Handles <see cref="GetRefreshSessionQuery"/>.</summary>
public sealed class GetRefreshSessionQueryHandler(IRefreshSessionRepository refreshSessionRepository)
    : IRequestHandler<GetRefreshSessionQuery, RefreshSessionDto?>
{
    /// <inheritdoc/>
    public async Task<RefreshSessionDto?> Handle(GetRefreshSessionQuery request, CancellationToken cancellationToken)
    {
        var refreshSession = await refreshSessionRepository.GetByIdAsync(new RefreshSessionId(request.RefreshSessionId), cancellationToken);

        return refreshSession is null ? null : RefreshSessionDto.FromDomain(refreshSession);
    }
}
