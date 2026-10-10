using System.Collections.Concurrent;

namespace Clovent.Platform.Printing;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IPrinterJobTracker"/>.
/// Maintains a bounded history of resolved print jobs for operational diagnostics and audit logging,
/// while never evicting unresolved jobs.
/// </summary>
public sealed class InMemoryPrinterJobTracker : IPrinterJobTracker
{
    private readonly ConcurrentDictionary<Guid, PrinterJob> _jobs = new();
    private readonly int _maxCapacity;

    /// <summary>Initializes the tracker with a maximum completed job retention capacity.</summary>
    public InMemoryPrinterJobTracker(int maxCapacity = 200)
    {
        _maxCapacity = maxCapacity;
    }

    /// <inheritdoc/>
    public void RecordJob(PrinterJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        _jobs[job.JobId] = job;

        // Prune oldest resolved entries if capacity exceeded; never evict unresolved work
        if (_jobs.Count > _maxCapacity)
        {
            var oldestResolved = _jobs.Values
                .Where(j => !j.IsUnresolved)
                .OrderBy(j => j.CreatedAtUtc)
                .Take(_jobs.Count - _maxCapacity)
                .ToList();

            foreach (var old in oldestResolved)
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
        }
    }

    /// <inheritdoc/>
    public void MarkInFlight(Guid jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = PrinterJobStatus.InFlight;
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
        }
    }

    /// <inheritdoc/>
    public void MarkConfirmed(Guid jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = PrinterJobStatus.Confirmed;
            job.CompletedAtUtc = DateTimeOffset.UtcNow;
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
        }
    }

    /// <inheritdoc/>
    public void MarkCancelled(Guid jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = PrinterJobStatus.Cancelled;
            job.CompletedAtUtc = DateTimeOffset.UtcNow;
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
