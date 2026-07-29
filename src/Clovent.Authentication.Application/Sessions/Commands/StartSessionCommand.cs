using Clovent.Authentication.Application.Sessions.Dtos;
using Clovent.Authentication.Sessions;
using Clovent.Authentication.Shared.ValueObjects;
using Clovent.Identity.Users;
using MediatR;

namespace Clovent.Authentication.Application.Sessions.Commands;

/// <summary>Starts a new session for an already-authenticated user.</summary>
public sealed record StartSessionCommand(Guid UserId, TimeSpan? IdleTimeout = null, string? IpAddress = null) : IRequest<SessionDto>;

/// <summary>Handles <see cref="StartSessionCommand"/>.</summary>
public sealed class StartSessionCommandHandler(ISessionRepository sessionRepository, TimeProvider timeProvider)
    : IRequestHandler<StartSessionCommand, SessionDto>
{
    private static readonly TimeSpan DefaultIdleTimeout = TimeSpan.FromMinutes(30);

    /// <inheritdoc/>
    public async Task<SessionDto> Handle(StartSessionCommand request, CancellationToken cancellationToken)
    {
        var ipAddress = request.IpAddress is null ? null : IpAddress.Create(request.IpAddress);

        var session = Session.Start(
            new UserId(request.UserId),
            request.IdleTimeout ?? DefaultIdleTimeout,
            timeProvider.GetUtcNow(),
            ipAddress);

        await sessionRepository.AddAsync(session, cancellationToken);

        return SessionDto.FromDomain(session);
    }
}
