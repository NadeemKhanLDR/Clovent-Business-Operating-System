namespace Clovent.Identity.Application.Authorization;

/// <summary>The "Menu authorization" deliverable: may a user see/click a given menu item (e.g. Shell navigation, Milestone 11)?</summary>
public interface IMenuAuthorizationPolicy
{
    /// <summary>Whether the user may view <paramref name="menuItemCode"/> - backed by the permission code <c>menu.{menuItemCode}</c>.</summary>
    Task<bool> CanViewMenuItemAsync(Guid userId, string menuItemCode, CancellationToken cancellationToken = default);
}
