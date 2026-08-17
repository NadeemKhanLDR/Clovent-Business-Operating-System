using Clovent.Identity.Application.Authorization;

namespace Clovent.Desktop.Navigation;

/// <summary>
/// The "permission-aware navigation" deliverable's actual logic, kept
/// separate from <see cref="Forms.Shell.MainForm"/>'s DevExpress UI construction
/// so it can be unit tested without a Windows Forms message loop: filters
/// <see cref="INavigationService.RegisteredKeys"/> down to the ones the
/// given user is allowed to see, via <see cref="IMenuAuthorizationPolicy"/>.
/// </summary>
public sealed class NavigationMenuBuilder(INavigationService navigationService, IMenuAuthorizationPolicy menuAuthorizationPolicy)
{
    /// <summary>Every registered navigation key the user is permitted to view, in registration order.</summary>
    public async Task<IReadOnlyList<string>> GetVisibleMenuKeysAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var visible = new List<string>();

        foreach (var key in navigationService.RegisteredKeys)
        {
            if (await menuAuthorizationPolicy.CanViewMenuItemAsync(userId, key, cancellationToken))
            {
                visible.Add(key);
            }
        }

        return visible;
    }
}
