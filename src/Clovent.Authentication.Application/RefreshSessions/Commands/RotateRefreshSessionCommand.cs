using Clovent.Authentication.Application.RefreshSessions.Dtos;
using Clovent.Authentication.RefreshSessions;
using MediatR;

namespace Clovent.Authentication.Application.RefreshSessions.Commands;

/// <summary>Consumes a refresh session and issues its single-use replacement.</summary>
public sealed record RotateRefreshSessionCommand(Guid RefreshSessionId, TimeSpan? NewLifetime = null) : IRequest<RefreshSessionDto>;

/// <summary>Handles <see cref="RotateRefreshSessionCommand"/>.</summary>
public sealed class RotateRefreshSessionCommandHandler(IRefreshSessionRepository refreshSessionRepository, TimeProvider timeProvider)
    : IRequestHandler<RotateRefreshSessionCommand, RefreshSessionDto>
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(7);

    /// <inheritdoc/>
    public async Task<RefreshSessionDto> Handle(RotateRefreshSessionCommand request, CancellationToken cancellationToken)
    {
        var refreshSessionId = new RefreshSessionId(request.RefreshSessionId);
        var refreshSession = await refreshSessionRepository.GetByIdAsync(refreshSessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(RefreshSession), refreshSessionId);

        var replacement = refreshSession.Rotate(request.NewLifetime ?? DefaultLifetime, timeProvider.GetUtcNow());
        await refreshSessionRepository.AddAsync(replacement, cancellationToken);

        return RefreshSessionDto.FromDomain(replacement);
    }
}
