using Clovent.Authentication.Application.Sessions.Dtos;
using Clovent.Authentication.Sessions;
using Clovent.Identity.Users;
using MediatR;

namespace Clovent.Authentication.Application.Sessions.Queries;

/// <summary>Retrieves every currently-active session for a user.</summary>
public sealed record GetActiveSessionsForUserQuery(Guid UserId) : IRequest<IReadOnlyCollection<SessionDto>>;

/// <summary>Handles <see cref="GetActiveSessionsForUserQuery"/>.</summary>
public sealed class GetActiveSessionsForUserQueryHandler(ISessionRepository sessionRepository)
    : IRequestHandler<GetActiveSessionsForUserQuery, IReadOnlyCollection<SessionDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<SessionDto>> Handle(GetActiveSessionsForUserQuery request, CancellationToken cancellationToken)
    {
        var sessions = await sessionRepository.GetActiveByUserIdAsync(new UserId(request.UserId), cancellationToken);

        return sessions.Select(SessionDto.FromDomain).ToList();
    }
}
