namespace Clovent.Identity.Application.Authorization;

/// <summary>The "Feature authorization" deliverable: may a user use a given feature/action (e.g. "export to Excel")?</summary>
public interface IFeatureAuthorizationPolicy
{
    /// <summary>Whether the user may use <paramref name="featureCode"/> - backed by the permission code <c>feature.{featureCode}</c>.</summary>
    Task<bool> CanUseFeatureAsync(Guid userId, string featureCode, CancellationToken cancellationToken = default);
}
