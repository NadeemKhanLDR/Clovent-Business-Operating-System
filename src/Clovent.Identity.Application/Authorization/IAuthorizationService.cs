namespace Clovent.Identity.Application.Authorization;

/// <summary>
/// Evaluates what a user is allowed to do - the "Permission evaluation" and
/// "Role evaluation" deliverables. <see cref="IModuleAuthorizationPolicy"/>/
/// <see cref="IMenuAuthorizationPolicy"/>/<see cref="IFeatureAuthorizationPolicy"/>
/// are thin, semantically-named wrappers over this single service (each
/// checks one permission-code convention) rather than three parallel
/// evaluation engines - there is exactly one way permissions are actually
/// checked, no duplicated logic across granularities.
/// </summary>
public interface IAuthorizationService
{
    /// <summary>Every permission code granted to the user, directly or via any assigned role - resolved through <see cref="IPermissionCache"/> where possible.</summary>
    Task<IReadOnlyCollection<string>> GetPermissionCodesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Whether the user holds <paramref name="permissionCode"/> via any assigned role.</summary>
    Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default);

    /// <summary>Whether the user has the named role assigned.</summary>
    Task<bool> HasRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);

    /// <summary>Whether the user holds every permission code required by the named <see cref="AuthorizationPolicy"/>. <see langword="false"/> if no such policy is registered.</summary>
    Task<bool> SatisfiesPolicyAsync(Guid userId, string policyName, CancellationToken cancellationToken = default);
}
