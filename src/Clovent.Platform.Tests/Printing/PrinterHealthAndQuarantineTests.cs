using System;
using System.IO;
using System.Threading.Tasks;
using Clovent.Platform.Printing;
using Xunit;

namespace Clovent.Platform.Tests.Printing;

public sealed class PrinterHealthAndQuarantineTests : IDisposable
{
    private readonly string _tempQuarantineDir;

    public PrinterHealthAndQuarantineTests()
    {
        _tempQuarantineDir = Path.Combine(Path.GetTempPath(), $"cbos-quarantine-test-{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempQuarantineDir))
            {
                Directory.Delete(_tempQuarantineDir, recursive: true);
            }
        }
        catch
        {
            // Ignore test cleanup exceptions
        }
    }

    [Fact]
    public void PrinterHardwareCondition_FlagsOperateCorrectly()
    {
        // Normal state is 0
        Assert.Equal(PrinterHardwareCondition.Normal, (PrinterHardwareCondition)0);

        // Multiple concurrent faults can be combined
        var combinedFault = PrinterHardwareCondition.CoverOpen | PrinterHardwareCondition.PaperOut;
        Assert.True(combinedFault.HasFlag(PrinterHardwareCondition.CoverOpen));
        Assert.True(combinedFault.HasFlag(PrinterHardwareCondition.PaperOut));
        Assert.False(combinedFault.HasFlag(PrinterHardwareCondition.CutterError));
        Assert.False(combinedFault.HasFlag(PrinterHardwareCondition.Offline));

        // Adding cutter error
        combinedFault |= PrinterHardwareCondition.CutterError;
        Assert.True(combinedFault.HasFlag(PrinterHardwareCondition.CutterError));
    }

    [Fact]
    public void PrinterHealthSnapshot_HealthyState_EvaluatesReadyWithoutFault()
    {
        var profileId = Guid.NewGuid();
        var snapshot = PrinterHealthSnapshot.Healthy("POS-80", profileId, spoolerQueueDepth: 2);

        Assert.Equal("POS-80", snapshot.PrinterName);
        Assert.Equal(profileId, snapshot.ProfileId);
        Assert.True(snapshot.IsAvailable);
        Assert.Equal(PrinterHardwareCondition.Normal, snapshot.Condition);
        Assert.Equal(2, snapshot.SpoolerQueueDepth);
        Assert.True(snapshot.IsReady);
        Assert.False(snapshot.HasFault);
        Assert.Contains("ready", snapshot.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PrinterHealthSnapshot_FaultedState_EvaluatesNotReadyWithFault()
    {
        var profileId = Guid.NewGuid();
        var condition = PrinterHardwareCondition.CoverOpen | PrinterHardwareCondition.PaperOut;
        var snapshot = PrinterHealthSnapshot.Faulted("POS-80", condition, "Lid open and roll empty", profileId, spoolerQueueDepth: 5);

        Assert.Equal("POS-80", snapshot.PrinterName);
        Assert.Equal(profileId, snapshot.ProfileId);
        Assert.False(snapshot.IsAvailable);
        Assert.Equal(condition, snapshot.Condition);
        Assert.Equal(5, snapshot.SpoolerQueueDepth);
        Assert.False(snapshot.IsReady);
        Assert.True(snapshot.HasFault);
        Assert.Equal("Lid open and roll empty", snapshot.StatusMessage);
    }

    [Fact]
    public async Task PrintJobQuarantineStore_QuarantineRetrieveUpdateAndDelete_WorksDurably()
    {
        var store = new PrintJobQuarantineStore(_tempQuarantineDir);
        var jobId = Guid.NewGuid();
        var profileId = Guid.NewGuid();

        var job = new QuarantinedPrintJob
        {
            JobId = jobId,
            CorrelationId = Guid.NewGuid(),
            DocumentType = "CustomerReceipt",
            TargetPrinterProfileId = profileId,
            TargetSystemPrinterName = "Thermal-Receipt-1",
            PayloadText = "RECEIPT CONTENT 100.00",
            FailureReason = "Thermal printer out of paper",
            QuarantinedAtUtc = DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        // 1. Initial count is 0
        Assert.Equal(0, await store.GetQuarantinedJobCountAsync());

        // 2. Quarantine job
        await store.QuarantineJobAsync(job);
        Assert.Equal(1, await store.GetQuarantinedJobCountAsync());

        // 3. Retrieve by ID
        var retrieved = await store.GetJobAsync(jobId);
        Assert.NotNull(retrieved);
        Assert.Equal(jobId, retrieved.JobId);
        Assert.Equal("Thermal-Receipt-1", retrieved.TargetSystemPrinterName);
        Assert.Equal("RECEIPT CONTENT 100.00", retrieved.PayloadText);
        Assert.Equal("Thermal printer out of paper", retrieved.FailureReason);
        Assert.Equal(0, retrieved.RetryCount);

        // 4. Update job retry metadata
        retrieved.RetryCount = 1;
        retrieved.LastRetryAtUtc = DateTimeOffset.UtcNow;
        retrieved.LastRetryError = "Still out of paper";
        await store.UpdateJobAsync(retrieved);

        var updated = await store.GetJobAsync(jobId);
        Assert.NotNull(updated);
        Assert.Equal(1, updated.RetryCount);
        Assert.Equal("Still out of paper", updated.LastRetryError);

        // 5. Delete job
        var deleted = await store.DeleteJobAsync(jobId);
        Assert.True(deleted);
        Assert.Equal(0, await store.GetQuarantinedJobCountAsync());

        // Deleting non-existent job returns false
        Assert.False(await store.DeleteJobAsync(jobId));
    }

    [Fact]
    public async Task PrintJobQuarantineStore_GetQuarantinedJobs_ReturnsSortedList()
    {
        var store = new PrintJobQuarantineStore(_tempQuarantineDir);

        var job1 = new QuarantinedPrintJob
        {
            JobId = Guid.NewGuid(),
            TargetSystemPrinterName = "Printer-A",
            PayloadText = "Slip 1",
            FailureReason = "Offline",
            QuarantinedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10)
        };

        var job2 = new QuarantinedPrintJob
        {
            JobId = Guid.NewGuid(),
            TargetSystemPrinterName = "Printer-B",
            PayloadText = "Slip 2",
            FailureReason = "Paper Out",
            QuarantinedAtUtc = DateTimeOffset.UtcNow
        };

        await store.QuarantineJobAsync(job1);
        await store.QuarantineJobAsync(job2);

        var jobs = await store.GetQuarantinedJobsAsync();
        Assert.Equal(2, jobs.Count);
        // Sorted descending by QuarantinedAtUtc: job2 should be first
        Assert.Equal(job2.JobId, jobs[0].JobId);
        Assert.Equal(job1.JobId, jobs[1].JobId);
    }

    [Fact]
    public void InMemoryPrinterJobTracker_MarkQuarantined_TransitionsToQuarantinedWithReason()
    {
        var tracker = new InMemoryPrinterJobTracker();
        var job = PrinterJob.Create("CustomerReceipt", Guid.NewGuid(), "POS-80", "Receipt Text");
        tracker.RecordJob(job);
        tracker.MarkQueued(job.JobId);

        tracker.MarkQuarantined(job.JobId, "Thermal cutter blade blocked");

        var tracked = tracker.GetJob(job.JobId);
        Assert.NotNull(tracked);
        Assert.Equal(PrinterJobStatus.Quarantined, tracked.Status);
        Assert.Equal("Thermal cutter blade blocked", tracked.ErrorMessage);
        Assert.NotNull(tracked.CompletedAtUtc);
    }
}
