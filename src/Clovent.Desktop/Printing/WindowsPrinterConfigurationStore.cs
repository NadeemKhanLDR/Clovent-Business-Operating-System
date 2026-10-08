using System.Text.Json;
using Clovent.Platform.Configuration;
using Clovent.Platform.Printing;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Printing;

/// <summary>
/// Persists printer profiles and scope assignments to printers.json
/// with ProgramData precedence, atomic replacement, and corruption protection.
/// </summary>
public sealed class WindowsPrinterConfigurationStore : IPrinterConfigurationStore
{
    private readonly string _configFilePath;
    private readonly ILogger<WindowsPrinterConfigurationStore>? _logger;
    private PrinterConfiguration? _cachedConfig;
    private readonly object _lock = new();

    /// <summary>
    /// Constructs the printer configuration store.
    /// </summary>
    /// <param name="customFilePath">Optional custom path (used in unit testing to isolate state).</param>
    /// <param name="logger">Optional logger.</param>
    public WindowsPrinterConfigurationStore(
        string? customFilePath = null,
        ILogger<WindowsPrinterConfigurationStore>? logger = null)
    {
        _logger = logger;
        _configFilePath = customFilePath ?? ResolveConfigFilePath();
    }

    private static string ResolveConfigFilePath()
    {
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var machinePath = Path.Combine(programData, "Clovent", "BusinessOperatingSystem", "Config", "printers.json");

        try
        {
            var dir = Path.GetDirectoryName(machinePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return machinePath;
        }
        catch
        {
            // Fallback to LocalAppData for non-elevated or restricted environments
            var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localData, "Clovent", "Clovent.BusinessOperatingSystem", "Config", "printers.json");
        }
    }

    /// <inheritdoc/>
    public PrinterConfiguration GetConfiguration()
    {
        lock (_lock)
        {
            if (_cachedConfig != null)
            {
                return _cachedConfig;
            }

            _cachedConfig = LoadInternal();
            return _cachedConfig;
        }
    }

    /// <inheritdoc/>
    public Task<PrinterConfiguration> LoadAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetConfiguration());
    }

    /// <inheritdoc/>
    public Task SaveAsync(PrinterConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        lock (_lock)
        {
            _cachedConfig = configuration;
            try
            {
                AtomicFileWriter.WriteJsonAtomic(_configFilePath, configuration);
                _logger?.LogInformation("Successfully saved printer configuration to {Path}", _configFilePath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to persist printer configuration to {Path}", _configFilePath);
                throw;
            }
        }

        return Task.CompletedTask;
    }

    private PrinterConfiguration LoadInternal()
    {
        if (!File.Exists(_configFilePath))
        {
            var defaultConfig = new PrinterConfiguration();
            return defaultConfig;
        }

        try
        {
            var json = File.ReadAllText(_configFilePath);
            var config = JsonSerializer.Deserialize<PrinterConfiguration>(json);
            return config ?? new PrinterConfiguration();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error reading printer configuration from {Path}. Using default empty configuration.", _configFilePath);
            return new PrinterConfiguration();
        }
    }
}
