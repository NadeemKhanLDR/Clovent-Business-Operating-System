using Clovent.Platform.Printing;
using Clovent.Platform.Tests.TestSupport;
using Xunit;

namespace Clovent.Platform.Tests.Printing;

public class DurableFilePrinterJobTrackerTests
{
    [Fact]
    public void RecordJob_PersistsJobAtomicallyToDisk()
    {
        using var tempDir = new TempDirectory();
        var tracker = new DurableFilePrinterJobTracker(tempDir.Path);

        var job = PrinterJob.Create(
            documentType: "CustomerReceipt",
            profileId: Guid.NewGuid(),
            systemPrinterName: "POS-80",
            payloadText: "Receipt payload",
            organizationId: Guid.NewGuid(),
            branchId: Guid.NewGuid(),
            terminalId: Guid.NewGuid(),
            isReprint: false,
            reprintCount: 1,
            reprintReason: null,
            documentReference: "ORD-999");

        // Act
        tracker.RecordJob(job);

        // Assert
        var retrieved = tracker.GetJob(job.JobId);
        Assert.NotNull(retrieved);
        Assert.Equal(job.JobId, retrieved.JobId);
        Assert.Equal("ORD-999", retrieved.DocumentReference);
        Assert.Equal(PrinterJobStatus.Requested, retrieved.Status);

        // Assert file exists on disk
        var expectedFile = Path.Combine(tempDir.Path, $"job_{job.JobId:N}.json");
        Assert.True(File.Exists(expectedFile));
    }

    [Fact]
    public void RestartRecovery_PreservesJobIdentityOrganizationTerminalAndReprintAudit()
    {
        using var tempDir = new TempDirectory();
        var orgId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        var job = PrinterJob.Create(
            documentType: "ReprintReceipt",
            profileId: Guid.NewGuid(),
            systemPrinterName: "POS-80",
            payloadText: "Original Sale Payload",
            organizationId: orgId,
            branchId: branchId,
            terminalId: terminalId,
            isReprint: true,
            reprintCount: 3,
            reprintReason: "Customer requested copy",
            correlationId: correlationId,
            documentReference: "ORD-1234");

        // Phase 1: Record and transition to Queued
        {
            var tracker1 = new DurableFilePrinterJobTracker(tempDir.Path);
            tracker1.RecordJob(job);
            tracker1.MarkQueued(job.JobId);
        }

        // Phase 2: Simulate complete process termination and restart
        {
            var tracker2 = new DurableFilePrinterJobTracker(tempDir.Path);
            var recovered = tracker2.GetJob(job.JobId);

            Assert.NotNull(recovered);
            Assert.Equal(job.JobId, recovered.JobId);
            Assert.Equal(correlationId, recovered.CorrelationId);
            Assert.Equal(orgId, recovered.OrganizationId);
            Assert.Equal(branchId, recovered.BranchId);
            Assert.Equal(terminalId, recovered.TerminalId);
            Assert.Equal("ORD-1234", recovered.DocumentReference);
            Assert.True(recovered.IsReprint);
            Assert.Equal(3, recovered.ReprintCount);
            Assert.Equal("Customer requested copy", recovered.ReprintReason);
            Assert.Equal(PrinterJobStatus.Queued, recovered.Status);
        }
    }

    [Fact]
    public void RestartRecovery_WhenInterruptedAfterSpoolerSubmission_RecoversJobAsAmbiguous()
    {
        using var tempDir = new TempDirectory();
        var job = PrinterJob.Create(
            documentType: "CustomerReceipt",
            profileId: Guid.NewGuid(),
            systemPrinterName: "POS-80",
            payloadText: "Payment Receipt",
            documentReference: "ORD-555");

        // Phase 1: Job was submitted to spooler, but process abruptly died before hardware confirmation
        {
            var tracker1 = new DurableFilePrinterJobTracker(tempDir.Path);
            tracker1.RecordJob(job);
            tracker1.MarkSubmitted(job.JobId);
            Assert.Equal(PrinterJobStatus.Submitted, tracker1.GetJob(job.JobId)!.Status);
        }

        // Phase 2: Host restarts - status must transition to Ambiguous to prevent false confirmation or automatic duplicate print
        {
            var tracker2 = new DurableFilePrinterJobTracker(tempDir.Path);
            var recovered = tracker2.GetJob(job.JobId);

            Assert.NotNull(recovered);
            Assert.Equal(PrinterJobStatus.Ambiguous, recovered.Status);
            Assert.Contains("ambiguous", recovered.ErrorMessage, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(recovered.CompletedAtUtc);

            // Unresolved query must return this ambiguous job for operator attention
            var unresolved = tracker2.GetUnresolvedJobs();
            Assert.Contains(unresolved, j => j.JobId == job.JobId);
        }
    }

    [Fact]
    public void CapacityPruning_NeverEvictsUnresolvedJobsMerelyBecauseBufferReachesCapacity()
    {
        using var tempDir = new TempDirectory();
        // Set capacity small (3) to test pruning logic
        var tracker = new DurableFilePrinterJobTracker(tempDir.Path, maxCompletedCapacity: 3);

        // Add 2 unresolved jobs
        var pending1 = PrinterJob.Create("Receipt", Guid.NewGuid(), "POS-80", "Pending 1");
        var pending2 = PrinterJob.Create("Receipt", Guid.NewGuid(), "POS-80", "Pending 2");
        tracker.RecordJob(pending1);
        tracker.RecordJob(pending2);
        tracker.MarkQueued(pending1.JobId);

        // Add 3 completed jobs
        for (int i = 0; i < 3; i++)
        {
            var done = PrinterJob.Create("Receipt", Guid.NewGuid(), "POS-80", $"Done {i}");
            tracker.RecordJob(done);
            tracker.MarkConfirmed(done.JobId);
        }

        // Assert: Unresolved jobs MUST still be present in memory and query
        Assert.NotNull(tracker.GetJob(pending1.JobId));
        Assert.NotNull(tracker.GetJob(pending2.JobId));

        var unresolved = tracker.GetUnresolvedJobs();
        Assert.Equal(2, unresolved.Count);
    }
}
