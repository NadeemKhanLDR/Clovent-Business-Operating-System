namespace Clovent.Identity.Application.Authorization;

/// <summary><see cref="IMenuAuthorizationPolicy"/> implementation - see <see cref="ModuleAuthorizationPolicy"/> for the identical reasoning.</summary>
public sealed class MenuAuthorizationPolicy(IAuthorizationService authorizationService) : IMenuAuthorizationPolicy
{
    /// <inheritdoc/>
    public Task<bool> CanViewMenuItemAsync(Guid userId, string menuItemCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(menuItemCode);
        return authorizationService.HasPermissionAsync(userId, $"menu.{menuItemCode.ToLowerInvariant()}", cancellationToken);
    }
}
