using Clovent.Identity.Permissions;

namespace Clovent.Identity.Application.Permissions.Dtos;

/// <summary>Read-model shape for a <see cref="Permission"/>, safe to cross a process boundary.</summary>
public sealed record PermissionDto(
    Guid PermissionId,
    string Code,
    string Description,
    DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="Permission"/> into its DTO.</summary>
    public static PermissionDto FromDomain(Permission permission) => new(
        permission.Id.Value,
        permission.Code.Value,
        permission.Description,
        permission.CreatedAtUtc);
}
