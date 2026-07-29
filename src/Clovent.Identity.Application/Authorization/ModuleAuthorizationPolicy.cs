namespace Clovent.Identity.Application.Authorization;

/// <summary><see cref="IModuleAuthorizationPolicy"/> implementation - a thin permission-code-prefix wrapper over <see cref="IAuthorizationService"/>, not a second evaluation engine.</summary>
public sealed class ModuleAuthorizationPolicy(IAuthorizationService authorizationService) : IModuleAuthorizationPolicy
{
    /// <inheritdoc/>
    public Task<bool> CanAccessModuleAsync(Guid userId, string moduleName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);
        return authorizationService.HasPermissionAsync(userId, $"module.{moduleName.ToLowerInvariant()}", cancellationToken);
    }
}
