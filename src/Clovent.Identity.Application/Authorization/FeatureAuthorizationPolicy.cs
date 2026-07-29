namespace Clovent.Identity.Application.Authorization;

/// <summary><see cref="IFeatureAuthorizationPolicy"/> implementation - see <see cref="ModuleAuthorizationPolicy"/> for the identical reasoning.</summary>
public sealed class FeatureAuthorizationPolicy(IAuthorizationService authorizationService) : IFeatureAuthorizationPolicy
{
    /// <inheritdoc/>
    public Task<bool> CanUseFeatureAsync(Guid userId, string featureCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureCode);
        return authorizationService.HasPermissionAsync(userId, $"feature.{featureCode.ToLowerInvariant()}", cancellationToken);
    }
}
