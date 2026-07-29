using Clovent.Identity.Application.Users.Dtos;
using Clovent.Identity.Branches;
using Clovent.Identity.Companies;
using Clovent.Identity.Roles;
using Clovent.Identity.Users;
using MediatR;

namespace Clovent.Identity.Application.Users.Queries;

/// <summary>
/// Retrieves users matching every supplied filter (all optional/AND'ed
/// together) - backs the User List screen's search box and Company/Branch/
/// Role/Status filters.
/// </summary>
public sealed record SearchUsersQuery(
    string? SearchText = null,
    Guid? CompanyId = null,
    Guid? BranchId = null,
    Guid? RoleId = null,
    UserStatus? Status = null) : IRequest<IReadOnlyCollection<UserDto>>;

/// <summary>Handles <see cref="SearchUsersQuery"/>.</summary>
public sealed class SearchUsersQueryHandler(IUserRepository userRepository)
    : IRequestHandler<SearchUsersQuery, IReadOnlyCollection<UserDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<UserDto>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await userRepository.SearchAsync(
            request.SearchText,
            request.CompanyId is { } companyId ? new CompanyId(companyId) : null,
            request.BranchId is { } branchId ? new BranchId(branchId) : null,
            request.RoleId is { } roleId ? new RoleId(roleId) : null,
            request.Status,
            cancellationToken);

        return [.. users.Select(UserDto.FromDomain)];
    }
}
