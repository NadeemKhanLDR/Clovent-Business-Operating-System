using System.Text.Json;
using Clovent.Platform.Configuration;
using Microsoft.Extensions.Logging;

namespace Clovent.Platform.Printing;

/// <summary>
/// Durable file-system implementation of <see cref="IPrintJobQuarantineStore"/>.
/// Persists quarantined print jobs into JSON documents under
/// %ProgramData%\Clovent\BusinessOperatingSystem\PrintJobs\quarantined\
/// with atomic write semantics to prevent file truncation during unexpected machine reboots or power loss.
/// </summary>
public sealed class PrintJobQuarantineStore : IPrintJobQuarantineStore
{
    private const string QuarantineFilePrefix = "quarantine_";
    private const string QuarantineFileExtension = ".json";

    private readonly string _quarantineDirectory;
    private readonly ILogger<PrintJobQuarantineStore>? _logger;
    private readonly object _lock = new();

    /// <inheritdoc/>
    public string QuarantineDirectory => _quarantineDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrintJobQuarantineStore"/> class.
    /// </summary>
    /// <param name="customDirectoryPath">Optional custom path for testing and test isolation.</param>
    /// <param name="logger">Optional logger instance.</param>
    public PrintJobQuarantineStore(
        string? customDirectoryPath = null,
        ILogger<PrintJobQuarantineStore>? logger = null)
    {
        _logger = logger;
        _quarantineDirectory = customDirectoryPath ?? ResolveDefaultQuarantineDirectory();
        EnsureDirectoryExists();
    }

    private static string ResolveDefaultQuarantineDirectory()
    {
        var commonPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var dir = Path.Combine(commonPath, "Clovent", "BusinessOperatingSystem", "PrintJobs", "quarantined");

        try
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }
        catch
        {
            // Fallback for restricted user environments
            var localPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localPath, "Clovent", "Clovent.BusinessOperatingSystem", "PrintJobs", "quarantined");
        }
    }

    private void EnsureDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(_quarantineDirectory))
            {
                Directory.CreateDirectory(_quarantineDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to create quarantine directory at {Directory}", _quarantineDirectory);
        }
    }

    private string GetFilePathForJob(Guid jobId) =>
        Path.Combine(_quarantineDirectory, $"{QuarantineFilePrefix}{jobId:N}{QuarantineFileExtension}");

    /// <inheritdoc/>
    public Task QuarantineJobAsync(QuarantinedPrintJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        lock (_lock)
        {
            EnsureDirectoryExists();
            var filePath = GetFilePathForJob(job.JobId);
            AtomicFileWriter.WriteJsonAtomic(filePath, job);
            _logger?.LogWarning("Quarantined print job {JobId} for queue '{Printer}' to {Path}. Reason: {Reason}",
                job.JobId, job.TargetSystemPrinterName, filePath, job.FailureReason);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<QuarantinedPrintJob>> GetQuarantinedJobsAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            EnsureDirectoryExists();

            if (!Directory.Exists(_quarantineDirectory))
            {
                return Task.FromResult<IReadOnlyList<QuarantinedPrintJob>>([]);
            }

            var files = Directory.GetFiles(_quarantineDirectory, $"{QuarantineFilePrefix}*{QuarantineFileExtension}");
            var list = new List<QuarantinedPrintJob>();

            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var job = JsonSerializer.Deserialize<QuarantinedPrintJob>(json);
                    if (job != null)
                    {
                        list.Add(job);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to read quarantined print job file {File}", file);
                }
            }

            var sorted = list.OrderByDescending(j => j.QuarantinedAtUtc).ToList();
            return Task.FromResult<IReadOnlyList<QuarantinedPrintJob>>(sorted);
        }
    }

    /// <inheritdoc/>
    public Task<QuarantinedPrintJob?> GetJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var filePath = GetFilePathForJob(jobId);
            if (!File.Exists(filePath))
            {
                return Task.FromResult<QuarantinedPrintJob?>(null);
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var job = JsonSerializer.Deserialize<QuarantinedPrintJob>(json);
                return Task.FromResult(job);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to read quarantined print job file {File}", filePath);
                return Task.FromResult<QuarantinedPrintJob?>(null);
            }
        }
    }

    /// <inheritdoc/>
    public Task UpdateJobAsync(QuarantinedPrintJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        lock (_lock)
        {
            EnsureDirectoryExists();
            var filePath = GetFilePathForJob(job.JobId);
            AtomicFileWriter.WriteJsonAtomic(filePath, job);
            _logger?.LogInformation("Updated quarantined print job {JobId} metadata at {Path}", job.JobId, filePath);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> DeleteJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var filePath = GetFilePathForJob(jobId);
            if (!File.Exists(filePath))
            {
                return Task.FromResult(false);
            }

            try
            {
                File.Delete(filePath);
                _logger?.LogInformation("Deleted recovered quarantined print job {JobId} from {Path}", jobId, filePath);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to delete quarantined print job file {Path}", filePath);
                return Task.FromResult(false);
            }
        }
    }

    /// <inheritdoc/>
    public Task<int> GetQuarantinedJobCountAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!Directory.Exists(_quarantineDirectory))
            {
                return Task.FromResult(0);
            }

            var count = Directory.GetFiles(_quarantineDirectory, $"{QuarantineFilePrefix}*{QuarantineFileExtension}").Length;
            return Task.FromResult(count);
        }
    }
}
