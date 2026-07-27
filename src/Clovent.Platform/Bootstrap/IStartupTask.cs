namespace Clovent.Platform.Bootstrap;

/// <summary>
/// Extension point for arbitrary future startup work beyond persistence
/// initialization (cache warm-up, background job registration, etc.), run
/// during <see cref="ApplicationBootstrapper.BuildAndInitializeAsync"/>.
/// Platform Foundation registers none of these itself.
/// </summary>
public interface IStartupTask
{
    /// <summary>
    /// Performs this module's startup work. Called once per registered
    /// implementation, after every <see cref="IPersistenceInitializer"/> has
    /// already run, so this can assume persistence is ready.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token propagated from <see cref="ApplicationBootstrapper.BuildAndInitializeAsync"/>.</param>
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
