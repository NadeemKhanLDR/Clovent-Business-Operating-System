namespace Clovent.Platform.Printing;

/// <summary>
/// Abstraction for loading and persisting printer profiles and assignments.
/// </summary>
public interface IPrinterConfigurationStore
{
    /// <summary>Retrieves the cached or current printer configuration synchronously.</summary>
    PrinterConfiguration GetConfiguration();

    /// <summary>Loads the printer configuration from persistent storage asynchronously.</summary>
    Task<PrinterConfiguration> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Saves the printer configuration to persistent storage atomically.</summary>
    Task SaveAsync(PrinterConfiguration configuration, CancellationToken cancellationToken = default);
}
