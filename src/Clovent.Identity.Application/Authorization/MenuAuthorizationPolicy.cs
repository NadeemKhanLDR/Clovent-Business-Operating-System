namespace Clovent.Identity.Application.Authorization;

/// <summary><see cref="IMenuAuthorizationPolicy"/> implementation - see <see cref="ModuleAuthorizationPolicy"/> for the identical reasoning.</summary>
public sealed class MenuAuthorizationPolicy(IAuthorizationService authorizationService) : IMenuAuthorizationPolicy
{
    /// <inheritdoc/>
    public async Task<bool> CanViewMenuItemAsync(Guid userId, string menuItemCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(menuItemCode);
        var normalized = menuItemCode.ToLowerInvariant();
        if (await authorizationService.HasPermissionAsync(userId, $"menu.{normalized}", cancellationToken).ConfigureAwait(false))
        {
            return true;
        }

        if (normalized is "auditanalytics" or "audit.analytics")
        {
            var alternate = normalized == "auditanalytics" ? "menu.audit.analytics" : "menu.auditanalytics";
            return await authorizationService.HasPermissionAsync(userId, alternate, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }
}
