namespace Clovent.Platform.Bootstrap;

/// <summary>
/// Extension point for arbitrary future startup work beyond persistence
/// initialization (cache warm-up, background job registration, etc.), run
/// during <see cref="ApplicationBootstrapper.BuildAndInitializeAsync"/>.
/// Platform Foundation registers none of these itself.
/// </summary>
public interface IStartupTask
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
