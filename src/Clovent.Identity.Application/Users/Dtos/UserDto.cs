using Clovent.Identity.Users;

namespace Clovent.Identity.Application.Users.Dtos;

/// <summary>Read-model shape for a <see cref="User"/>, safe to cross a process boundary.</summary>
public sealed record UserDto(
    Guid UserId,
    string Email,
    string UserName,
    string DisplayName,
    string Status,
    Guid? CompanyId,
    Guid? BranchId,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<Guid> RoleIds)
{
    /// <summary>Projects a domain <see cref="User"/> into its DTO.</summary>
    public static UserDto FromDomain(User user) => new(
        user.Id.Value,
        user.Email.Value,
        user.UserName.Value,
        user.DisplayName.Value,
        user.Status.ToString(),
        user.CompanyId?.Value,
        user.BranchId?.Value,
        user.CreatedAtUtc,
        [.. user.RoleIds.Select(id => id.Value)]);
}
