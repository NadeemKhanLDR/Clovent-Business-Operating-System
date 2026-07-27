namespace Clovent.Platform.Bootstrap;

/// <summary>
/// Extension point for a module's persistence initialization (applying EF
/// Core migrations, etc.), run during <see cref="ApplicationBootstrapper.BuildAndInitializeAsync"/>.
/// Platform Foundation registers none of these itself - modules register
/// their own via DI (`services.AddSingleton&lt;IPersistenceInitializer, X&gt;()`
/// inside their own AddPersistence()), and the bootstrap pipeline discovers
/// and runs whatever has been registered, with no switch statement.
/// </summary>
public interface IPersistenceInitializer
{
    /// <summary>
    /// Prepares this module's persistence for use (e.g. applying pending EF
    /// Core migrations). Called once per registered implementation, before
    /// any <see cref="IStartupTask"/> runs, so startup tasks can assume
    /// schema is ready.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token propagated from <see cref="ApplicationBootstrapper.BuildAndInitializeAsync"/>.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
