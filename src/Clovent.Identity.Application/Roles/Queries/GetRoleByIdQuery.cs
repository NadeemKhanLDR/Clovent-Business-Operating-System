using Clovent.Identity.Application.Roles.Dtos;
using Clovent.Identity.Roles;
using MediatR;

namespace Clovent.Identity.Application.Roles.Queries;

/// <summary>Retrieves a single role by identity.</summary>
public sealed record GetRoleByIdQuery(Guid RoleId) : IRequest<RoleDto>;

/// <summary>Handles <see cref="GetRoleByIdQuery"/>.</summary>
public sealed class GetRoleByIdQueryHandler(IRoleRepository roleRepository)
    : IRequestHandler<GetRoleByIdQuery, RoleDto>
{
    /// <inheritdoc/>
    public async Task<RoleDto> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(new RoleId(request.RoleId), cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.RoleId);

        return RoleDto.FromDomain(role);
    }
}
