namespace Clovent.Identity.Application.Authorization;

/// <summary>The "Module authorization" deliverable: can a user access a given business module (e.g. "RestaurantPOS")?</summary>
public interface IModuleAuthorizationPolicy
{
    /// <summary>Whether the user may access <paramref name="moduleName"/> - backed by the permission code <c>module.{moduleName}</c>.</summary>
    Task<bool> CanAccessModuleAsync(Guid userId, string moduleName, CancellationToken cancellationToken = default);
}
