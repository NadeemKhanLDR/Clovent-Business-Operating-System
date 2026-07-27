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
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
