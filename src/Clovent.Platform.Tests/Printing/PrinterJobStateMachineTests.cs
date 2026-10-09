using Clovent.Platform.Printing;
using Xunit;

namespace Clovent.Platform.Tests.Printing;

public class PrinterJobStateMachineTests
{
    [Fact]
    public void RecordJob_CreatesJobInRequestedState()
    {
        // Arrange
        var tracker = new InMemoryPrinterJobTracker();
        var job = PrinterJob.Create("CustomerReceipt", Guid.NewGuid(), "POS-80", "Sample Receipt Text");

        // Act
        tracker.RecordJob(job);
        var retrieved = tracker.GetJob(job.JobId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(PrinterJobStatus.Requested, retrieved.Status);
    }

    [Fact]
    public void StateTransitions_FollowCorrectLifecycle()
    {
        // Arrange
        var tracker = new InMemoryPrinterJobTracker();
        var job = PrinterJob.Create("CustomerReceipt", Guid.NewGuid(), "POS-80", "Sample Text");
        tracker.RecordJob(job);

        // Act & Assert 1: Queued
        tracker.MarkQueued(job.JobId);
        Assert.Equal(PrinterJobStatus.Queued, tracker.GetJob(job.JobId)!.Status);

        // Act & Assert 2: Submitted (Spooler Acceptance)
        tracker.MarkSubmitted(job.JobId);
        var submittedJob = tracker.GetJob(job.JobId)!;
        Assert.Equal(PrinterJobStatus.Submitted, submittedJob.Status);
        Assert.NotNull(submittedJob.SubmittedAtUtc);

        // Act & Assert 3: Confirmed (Hardware Output Acknowledged)
        tracker.MarkConfirmed(job.JobId);
        var confirmedJob = tracker.GetJob(job.JobId)!;
        Assert.Equal(PrinterJobStatus.Confirmed, confirmedJob.Status);
        Assert.NotNull(confirmedJob.CompletedAtUtc);
    }

    [Fact]
    public void MarkFailed_RecordsActionableErrorMessageAndTimestamp()
    {
        // Arrange
        var tracker = new InMemoryPrinterJobTracker();
        var job = PrinterJob.Create("CustomerReceipt", Guid.NewGuid(), "POS-80", "Sample Text");
        tracker.RecordJob(job);

        // Act
        tracker.MarkFailed(job.JobId, "Thermal head overheated or paper roll empty.");
        var failedJob = tracker.GetJob(job.JobId)!;

        // Assert
        Assert.Equal(PrinterJobStatus.Failed, failedJob.Status);
        Assert.Equal("Thermal head overheated or paper roll empty.", failedJob.ErrorMessage);
        Assert.NotNull(failedJob.CompletedAtUtc);
    }

    [Fact]
    public void CircularBuffer_EvictsOldestResolvedWhenCapacityExceeded()
    {
        // Arrange: capacity = 5
        var tracker = new InMemoryPrinterJobTracker(maxCapacity: 5);
        var firstJob = PrinterJob.Create("TestPrint", Guid.NewGuid(), "POS-80", "First");
        tracker.RecordJob(firstJob);
        tracker.MarkConfirmed(firstJob.JobId);

        // Act: Add and confirm 6 more jobs
        for (int i = 0; i < 6; i++)
        {
            var next = PrinterJob.Create("TestPrint", Guid.NewGuid(), "POS-80", $"Job {i}");
            tracker.RecordJob(next);
            tracker.MarkConfirmed(next.JobId);
        }

        // Assert: firstJob (resolved) should have been evicted
        Assert.Null(tracker.GetJob(firstJob.JobId));
        Assert.Equal(5, tracker.GetRecentJobs(10).Count);
    }

    [Fact]
    public void CircularBuffer_DoesNotEvictUnresolvedJobs_WhenCapacityExceeded()
    {
        // Arrange: capacity = 3
        var tracker = new InMemoryPrinterJobTracker(maxCapacity: 3);
        var unresolvedJob = PrinterJob.Create("TestPrint", Guid.NewGuid(), "POS-80", "Unresolved");
        tracker.RecordJob(unresolvedJob); // Status: Requested (Unresolved)

        // Act: Add 5 resolved jobs
        for (int i = 0; i < 5; i++)
        {
            var next = PrinterJob.Create("TestPrint", Guid.NewGuid(), "POS-80", $"Job {i}");
            tracker.RecordJob(next);
            tracker.MarkConfirmed(next.JobId);
        }

        // Assert: unresolvedJob must NOT be evicted merely because buffer reached capacity
        Assert.NotNull(tracker.GetJob(unresolvedJob.JobId));
        Assert.True(tracker.GetJob(unresolvedJob.JobId)!.IsUnresolved);
    }
}
