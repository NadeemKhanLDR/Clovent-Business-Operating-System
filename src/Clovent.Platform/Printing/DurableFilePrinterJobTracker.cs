using System.Collections.Concurrent;
using System.Text.Json;
using Clovent.Platform.Configuration;
using Microsoft.Extensions.Logging;

namespace Clovent.Platform.Printing;

/// <summary>
/// Durable file-based implementation of <see cref="IPrinterJobTracker"/>.
/// Atomically persists print jobs to disk so that job identity, organization, terminal,
/// document/snapshot reference, routing, attempt state, and reprint audit survive process restarts.
/// Tracks in-memory state for instantaneous queries, never evicts unresolved work,
/// and recovers interrupted spooler submissions into an explicit Ambiguous state.
/// </summary>
public sealed class DurableFilePrinterJobTracker : IPrinterJobTracker
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _storageDirectory;
    private readonly int _maxCompletedCapacity;
    private readonly ILogger<DurableFilePrinterJobTracker>? _logger;
    private readonly ConcurrentDictionary<Guid, PrinterJob> _jobs = new();
    private readonly object _diskLock = new();

    /// <summary>Initializes the durable file-based printer job tracker.</summary>
    /// <param name="customDirectory">Optional custom directory path (for test isolation).</param>
    /// <param name="maxCompletedCapacity">Maximum number of completed/historical jobs to keep in the in-memory cache.</param>
    /// <param name="logger">Optional logger instance.</param>
    public DurableFilePrinterJobTracker(
        string? customDirectory = null,
        int maxCompletedCapacity = 200,
        ILogger<DurableFilePrinterJobTracker>? logger = null)
    {
        _maxCompletedCapacity = maxCompletedCapacity;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(customDirectory))
        {
            _storageDirectory = customDirectory;
        }
        else
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (string.IsNullOrWhiteSpace(programData))
            {
                programData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }
            _storageDirectory = Path.Combine(programData, "Clovent", "BusinessOperatingSystem", "PrintJobs");
        }

        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
        }

        RecoverJobsOnStartup();
    }

    /// <summary>The directory path where print jobs are persisted.</summary>
    public string StorageDirectory => _storageDirectory;

    private void RecoverJobsOnStartup()
    {
        lock (_diskLock)
        {
            try
            {
                var files = Directory.GetFiles(_storageDirectory, "job_*.json");
                foreach (var file in files)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var job = JsonSerializer.Deserialize<PrinterJob>(json, JsonOpts);
                        if (job == null) continue;

                        // Governance rule: If a job was Submitted or InFlight when the process restarted,
                        // operating system spooler acceptance or physical output is uncertain.
                        // Must NOT be automatically replayed as exactly-once printing; mark as Ambiguous.
                        if (job.Status is PrinterJobStatus.Submitted or PrinterJobStatus.InFlight)
                        {
                            job.Status = PrinterJobStatus.Ambiguous;
                            job.ErrorMessage = job.Status == PrinterJobStatus.InFlight
                                ? "Interrupted across restart during active dispatch. Spooler outcome is uncertain; manual inspection required."
                                : "Interrupted across restart after spooler submission. Status is ambiguous; manual inspection required.";
                            job.CompletedAtUtc = DateTimeOffset.UtcNow;
                            AtomicFileWriter.WriteJsonAtomic(file, job, JsonOpts);
                            _logger?.LogWarning("Recovered interrupted print job {JobId} into Ambiguous status.", job.JobId);
                        }

                        _jobs[job.JobId] = job;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to recover print job file {File}", file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error scanning print job directory {Dir} during startup recovery.", _storageDirectory);
            }
        }
    }

    private string GetJobFilePath(Guid jobId) =>
        Path.Combine(_storageDirectory, $"job_{jobId:N}.json");

    private void PersistJob(PrinterJob job)
    {
        lock (_diskLock)
        {
            var path = GetJobFilePath(job.JobId);
            AtomicFileWriter.WriteJsonAtomic(path, job, JsonOpts);
        }
    }

    /// <inheritdoc/>
    public void RecordJob(PrinterJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        _jobs[job.JobId] = job;
        PersistJob(job);

        // Enforce capacity on resolved/completed jobs only
        if (_jobs.Count > _maxCompletedCapacity)
        {
            // Governance invariant: Never evict unresolved work (Requested, Queued, Ambiguous)
            var resolvedOldest = _jobs.Values
                .Where(j => !j.IsUnresolved)
                .OrderBy(j => j.CreatedAtUtc)
                .Take(_jobs.Count - _maxCompletedCapacity)
                .ToList();

            foreach (var old in resolvedOldest)
            {
                _jobs.TryRemove(old.JobId, out _);
            }
        }
    }

    /// <inheritdoc/>
    public PrinterJob? GetJob(Guid jobId) =>
        _jobs.TryGetValue(jobId, out var job) ? job : null;

    /// <inheritdoc/>
    public IReadOnlyList<PrinterJob> GetRecentJobs(int limit = 50) =>
        _jobs.Values
            .OrderByDescending(j => j.CreatedAtUtc)
            .Take(limit)
            .ToList();

    /// <inheritdoc/>
    public IReadOnlyList<PrinterJob> GetUnresolvedJobs() =>
        _jobs.Values
            .Where(j => j.IsUnresolved)
            .OrderBy(j => j.CreatedAtUtc)
            .ToList();

    /// <inheritdoc/>
    public void MarkQueued(Guid jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = PrinterJobStatus.Queued;
            PersistJob(job);
        }
    }

    /// <inheritdoc/>
    public void MarkInFlight(Guid jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = PrinterJobStatus.InFlight;
            PersistJob(job);
        }
    }

    /// <inheritdoc/>
    public void MarkSubmitted(Guid jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = PrinterJobStatus.Submitted;
            job.SubmittedAtUtc = DateTimeOffset.UtcNow;
            job.AttemptCount++;
            PersistJob(job);
        }
    }

    /// <inheritdoc/>
    public void MarkConfirmed(Guid jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = PrinterJobStatus.Confirmed;
            job.CompletedAtUtc = DateTimeOffset.UtcNow;
            PersistJob(job);
        }
    }

    /// <inheritdoc/>
    public void MarkFailed(Guid jobId, string errorMessage)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = PrinterJobStatus.Failed;
            job.ErrorMessage = errorMessage;
            job.CompletedAtUtc = DateTimeOffset.UtcNow;
            PersistJob(job);
        }
    }

    /// <inheritdoc/>
    public void MarkCancelled(Guid jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = PrinterJobStatus.Cancelled;
            job.CompletedAtUtc = DateTimeOffset.UtcNow;
            PersistJob(job);
        }
    }

    /// <inheritdoc/>
    public void MarkAmbiguous(Guid jobId, string reason)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = PrinterJobStatus.Ambiguous;
            job.ErrorMessage = reason;
            job.CompletedAtUtc = DateTimeOffset.UtcNow;
            PersistJob(job);
        }
    }

    /// <inheritdoc/>
    public void MarkQuarantined(Guid jobId, string errorMessage)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = PrinterJobStatus.Quarantined;
            job.ErrorMessage = errorMessage;
            job.CompletedAtUtc = DateTimeOffset.UtcNow;
            PersistJob(job);
        }
    }

    /// <inheritdoc/>
    public int GetReprintCount(string documentReference, Guid? organizationId = null)
    {
        if (string.IsNullOrWhiteSpace(documentReference)) return 0;
        return _jobs.Values.Count(j =>
            string.Equals(j.DocumentReference, documentReference, StringComparison.OrdinalIgnoreCase) &&
            j.IsReprint &&
            (!organizationId.HasValue || j.OrganizationId == organizationId.Value));
    }

    /// <inheritdoc/>
    public int GetReprintCount(Guid documentId, Guid? organizationId = null)
    {
        if (documentId == Guid.Empty) return 0;
        return _jobs.Values.Count(j =>
            j.DocumentId == documentId &&
            j.IsReprint &&
            (!organizationId.HasValue || j.OrganizationId == organizationId.Value));
    }
}
