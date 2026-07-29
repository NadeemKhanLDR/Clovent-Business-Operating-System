using Clovent.Identity.Roles;

namespace Clovent.Identity.Application.Roles.Dtos;

/// <summary>Read-model shape for a <see cref="Role"/>, safe to cross a process boundary.</summary>
public sealed record RoleDto(
    Guid RoleId,
    string Name,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<Guid> PermissionIds)
{
    /// <summary>Projects a domain <see cref="Role"/> into its DTO.</summary>
    public static RoleDto FromDomain(Role role) => new(
        role.Id.Value,
        role.Name.Value,
        role.CreatedAtUtc,
        [.. role.PermissionIds.Select(id => id.Value)]);
}
