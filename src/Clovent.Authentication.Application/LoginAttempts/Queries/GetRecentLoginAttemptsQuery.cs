using Clovent.Authentication.Application.LoginAttempts.Dtos;
using Clovent.Authentication.LoginAttempts;
using MediatR;

namespace Clovent.Authentication.Application.LoginAttempts.Queries;

/// <summary>Retrieves login attempts recorded against an identifier within a trailing time window.</summary>
public sealed record GetRecentLoginAttemptsQuery(string AttemptedIdentifier, TimeSpan Window) : IRequest<IReadOnlyCollection<LoginAttemptDto>>;

/// <summary>Handles <see cref="GetRecentLoginAttemptsQuery"/>.</summary>
public sealed class GetRecentLoginAttemptsQueryHandler(ILoginAttemptRepository loginAttemptRepository, TimeProvider timeProvider)
    : IRequestHandler<GetRecentLoginAttemptsQuery, IReadOnlyCollection<LoginAttemptDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<LoginAttemptDto>> Handle(GetRecentLoginAttemptsQuery request, CancellationToken cancellationToken)
    {
        var since = timeProvider.GetUtcNow() - request.Window;
        var attempts = await loginAttemptRepository.GetRecentByIdentifierAsync(request.AttemptedIdentifier, since, cancellationToken);

        return attempts.Select(LoginAttemptDto.FromDomain).ToList();
    }
}
