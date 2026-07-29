using Clovent.Identity.Application.Users.Dtos;
using Clovent.Identity.Users;
using MediatR;

namespace Clovent.Identity.Application.Users.Queries;

/// <summary>Retrieves a single user by identity.</summary>
public sealed record GetUserByIdQuery(Guid UserId) : IRequest<UserDto>;

/// <summary>Handles <see cref="GetUserByIdQuery"/>.</summary>
public sealed class GetUserByIdQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetUserByIdQuery, UserDto>
{
    /// <inheritdoc/>
    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(new UserId(request.UserId), cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        return UserDto.FromDomain(user);
    }
}
