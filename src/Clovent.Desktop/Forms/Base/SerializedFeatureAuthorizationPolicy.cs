using Clovent.Identity.Application.Authorization;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Decorates a scope's <see cref="IFeatureAuthorizationPolicy"/> so every
/// <c>CanUseFeatureAsync</c> call made from a screen's own UI code is
/// serialized through that screen's <see cref="ScreenOperationGate"/> -
/// the same gate <see cref="SerializedMediator"/> uses. See
/// <see cref="SerializedMediator"/>'s doc comment for why a screen's
/// mediator and feature-policy calls must share one gate, not one each.
/// </summary>
public sealed class SerializedFeatureAuthorizationPolicy(IFeatureAuthorizationPolicy inner, ScreenOperationGate gate) : IFeatureAuthorizationPolicy
{
    /// <inheritdoc/>
    public Task<bool> CanUseFeatureAsync(Guid userId, string featureCode, CancellationToken cancellationToken = default) =>
        gate.RunAsync(() => inner.CanUseFeatureAsync(userId, featureCode, cancellationToken), cancellationToken);
}
