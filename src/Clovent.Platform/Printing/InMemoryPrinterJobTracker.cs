using System.Collections.Concurrent;

namespace Clovent.Platform.Printing;

/// <summary>
/// Thread-safe in-memory circular buffer implementation of <see cref="IPrinterJobTracker"/>.
/// </summary>
public sealed class InMemoryPrinterJobTracker : IPrinterJobTracker
{
    private readonly ConcurrentDictionary<Guid, PrinterJob> _jobs = new();
    private readonly int _maxCapacity;

    /// <summary>Creates an in-memory job tracker with the specified history buffer capacity.</summary>
    public InMemoryPrinterJobTracker(int maxCapacity = 200)
    {
        _maxCapacity = maxCapacity;
    }

    /// <inheritdoc/>
    public void RecordJob(PrinterJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        _jobs[job.JobId] = job;

        if (_jobs.Count > _maxCapacity)
        {
            var oldest = _jobs.Values
                .OrderBy(j => j.CreatedAtUtc)
                .Take(_jobs.Count - _maxCapacity);

            foreach (var old in oldest)
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
    public void MarkQueued(Guid jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = PrinterJobStatus.Queued;
        }
    }

    /// <inheritdoc/>
    public void MarkSubmitted(Guid jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = PrinterJobStatus.Submitted;
            job.SubmittedAtUtc = DateTimeOffset.UtcNow;
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
}
