using System.IO;
using System.Security.Cryptography;
using System.Text;
using Clovent.Desktop.Commissioning.Services;
using Clovent.Desktop.Licensing;
using Xunit;

namespace Clovent.Desktop.Tests.Licensing;

/// <summary>
/// Comprehensive verification suite for 30-Day Evaluation / Trial Mode lifecycle,
/// persistence, anti-tamper, and commercial superseding per Part A1 requirements.
/// </summary>
public sealed class TrialStateManagerTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _testMachineId = "1111-2222-3333-4444";
    private readonly DateTimeOffset _baseTime = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    public TrialStateManagerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"cbos_trial_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        TrialStateManager.SetTestingOverrides(_testDir, () => _baseTime, _testMachineId);
        LicenseService.ResetTestingOverrides();
    }

    public void Dispose()
    {
        TrialStateManager.ResetTestingOverrides();
        LicenseService.ResetTestingOverrides();
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch
        {
            // Ignore temp dir cleanup errors
        }
    }

    // 1. Clean installation (not started)
    [Fact]
    public void CleanInstallation_WithoutCommissioningMarker_ReturnsNotStarted()
    {
        var result = TrialStateManager.EvaluateTrial();

        Assert.Equal(TrialStateStatus.NotStarted, result.Status);
        Assert.False(result.IsActive);
        Assert.Equal(0, result.DaysRemaining);
    }

    // 2. Start trial on Day 1
    [Fact]
    public void StartTrial_InitializesActiveTrialWith30Days()
    {
        var result = TrialStateManager.StartTrial(_testMachineId, _baseTime);

        Assert.Equal(TrialStateStatus.Active, result.Status);
        Assert.True(result.IsActive);
        Assert.Equal(30, result.DaysRemaining);
        Assert.Equal(_baseTime, result.StartedAtUtc);
        Assert.Equal(_baseTime.AddDays(30), result.ExpiryDate);
        Assert.Contains("30 day(s) remaining", result.StatusMessage);

        // Verify state file was created
        var primaryPath = TrialStateManager.GetPrimaryTrialStatePath();
        Assert.True(File.Exists(primaryPath));
    }

    // 3. Application restart simulation
    [Fact]
    public void ApplicationRestart_SimulatedByReload_PreservesActiveState()
    {
        TrialStateManager.StartTrial(_testMachineId, _baseTime);

        // Advance clock by 6 hours
        var restartTime = _baseTime.AddHours(6);
        TrialStateManager.SetTestingOverrides(_testDir, () => restartTime, _testMachineId);

        var result = TrialStateManager.EvaluateTrial();

        Assert.Equal(TrialStateStatus.Active, result.Status);
        Assert.True(result.IsActive);
        Assert.Equal(30, result.DaysRemaining);
    }

    // 4. Trial remains active at Day 15
    [Fact]
    public void Day15_TrialRemainsActiveWith15DaysRemaining()
    {
        TrialStateManager.StartTrial(_testMachineId, _baseTime);

        // Advance clock by 15 days
        var day15 = _baseTime.AddDays(15);
        TrialStateManager.SetTestingOverrides(_testDir, () => day15, _testMachineId);

        var result = TrialStateManager.EvaluateTrial();

        Assert.Equal(TrialStateStatus.Active, result.Status);
        Assert.True(result.IsActive);
        Assert.Equal(15, result.DaysRemaining);
    }

    // 5. Day 29 (1 day remaining)
    [Fact]
    public void Day29_TrialRemainsActiveWith1DayRemaining()
    {
        TrialStateManager.StartTrial(_testMachineId, _baseTime);

        // Advance clock by 29 days
        var day29 = _baseTime.AddDays(29);
        TrialStateManager.SetTestingOverrides(_testDir, () => day29, _testMachineId);

        var result = TrialStateManager.EvaluateTrial();

        Assert.Equal(TrialStateStatus.Active, result.Status);
        Assert.True(result.IsActive);
        Assert.Equal(1, result.DaysRemaining);
    }

    // 6. Day 31 (Expired)
    [Fact]
    public void Day31_TrialExpiresGracefullyWithoutDataDestruction()
    {
        TrialStateManager.StartTrial(_testMachineId, _baseTime);

        // Advance clock by 31 days
        var day31 = _baseTime.AddDays(31);
        TrialStateManager.SetTestingOverrides(_testDir, () => day31, _testMachineId);

        var result = TrialStateManager.EvaluateTrial();

        Assert.Equal(TrialStateStatus.Expired, result.Status);
        Assert.False(result.IsActive);
        Assert.Equal(0, result.DaysRemaining);
        Assert.Contains("expired", result.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    // 7. System clock rollback detection
    [Fact]
    public void ClockRollback_DetectedAndBlocksTrialActivation()
    {
        TrialStateManager.StartTrial(_testMachineId, _baseTime);

        // Advance clock to Day 10
        var day10 = _baseTime.AddDays(10);
        TrialStateManager.SetTestingOverrides(_testDir, () => day10, _testMachineId);
        var res10 = TrialStateManager.EvaluateTrial();
        Assert.True(res10.IsActive);

        // Now roll clock back to Day 5 (5 days earlier than last verified)
        var rolledBack = _baseTime.AddDays(5);
        TrialStateManager.SetTestingOverrides(_testDir, () => rolledBack, _testMachineId);

        var rollbackResult = TrialStateManager.EvaluateTrial();

        Assert.Equal(TrialStateStatus.ClockRollback, rollbackResult.Status);
        Assert.False(rollbackResult.IsActive);
        Assert.Contains("clock rollback", rollbackResult.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    // 8. Corrupted trial state detection
    [Fact]
    public void CorruptedTrialState_HMACMismatchDetected()
    {
        TrialStateManager.StartTrial(_testMachineId, _baseTime);

        // Load the state, tamper with duration, but keep old HMAC
        var loaded = TrialStateManager.LoadTrialState();
        Assert.NotNull(loaded);
        loaded.DurationDays = 365; // Tamper to 1 year!
        TrialStateManager.SaveTrialStateRawForTest(loaded);

        var result = TrialStateManager.EvaluateTrial();

        Assert.Equal(TrialStateStatus.Corrupted, result.Status);
        Assert.False(result.IsActive);
        Assert.Contains("corruption", result.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    // 9. Hardware ID mismatch / node locking
    [Fact]
    public void MachineMismatch_DetectedWhenMovedToDifferentHardware()
    {
        TrialStateManager.StartTrial(_testMachineId, _baseTime);

        // Change simulated machine ID
        TrialStateManager.SetTestingOverrides(_testDir, () => _baseTime.AddDays(1), "9999-8888-7777-6666");

        var result = TrialStateManager.EvaluateTrial();

        Assert.Equal(TrialStateStatus.MachineMismatch, result.Status);
        Assert.False(result.IsActive);
        Assert.Contains("bound to machine", result.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    // 10. Reinstall / re-run cannot reset trial start date
    [Fact]
    public void ReinstallOrRestart_CannotResetTrialStartDate()
    {
        TrialStateManager.StartTrial(_testMachineId, _baseTime);

        // Advance 10 days
        var day10 = _baseTime.AddDays(10);
        TrialStateManager.SetTestingOverrides(_testDir, () => day10, _testMachineId);

        // Customer attempts to call StartTrial again with new start date
        var restartedResult = TrialStateManager.StartTrial(_testMachineId, day10);

        // Start date must remain the original _baseTime!
        Assert.Equal(_baseTime, restartedResult.StartedAtUtc);
        Assert.Equal(20, restartedResult.DaysRemaining);
    }

    // 11. Deleted trial state restores from CommissioningMarker
    [Fact]
    public void DeletedTrialState_RestoresFromCommissioningMarker()
    {
        // Start trial
        TrialStateManager.StartTrial(_testMachineId, _baseTime);

        // Advance 5 days
        var day5 = _baseTime.AddDays(5);

        // Delete the trial state file
        var primaryPath = TrialStateManager.GetPrimaryTrialStatePath();
        if (File.Exists(primaryPath)) File.Delete(primaryPath);

        // Provide commissioning marker with original commissioning timestamp
        var marker = new CommissioningMarker
        {
            MachineId = _testMachineId,
            CommissionedAtUtc = _baseTime,
            IsEvaluation = true
        };
        TrialStateManager.SetTestingOverrides(_testDir, () => day5, _testMachineId, () => marker);

        var restoredResult = TrialStateManager.EvaluateTrial();

        Assert.Equal(TrialStateStatus.Active, restoredResult.Status);
        Assert.True(restoredResult.IsActive);
        Assert.Equal(_baseTime, restoredResult.StartedAtUtc);
        Assert.Equal(25, restoredResult.DaysRemaining);
    }

    // 12. Commercial license installation permanently supersedes trial
    [Fact]
    public void CommercialLicenseInstalled_PermanentlySupersedesTrial()
    {
        TrialStateManager.StartTrial(_testMachineId, _baseTime);

        // Record commercial license installation
        TrialStateManager.RecordCommercialLicenseInstalled();

        var result = TrialStateManager.EvaluateTrial();

        Assert.Equal(TrialStateStatus.CommercialSuperseded, result.Status);
        Assert.False(result.IsActive);
        Assert.Contains("Commercial license", result.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    // 13. LicenseService integration in Trial Mode
    [Fact]
    public void LicenseService_ValidateCurrentLicense_ReturnsValidEvaluationWhenInTrial()
    {
        TrialStateManager.StartTrial(_testMachineId, _baseTime);
        LicenseService.SetLicenseFilePathForTesting(null); // Ensure no clovent.lic is loaded

        var result = LicenseService.ValidateCurrentLicense();

        Assert.Equal(LicenseStatus.Valid, result.Status);
        Assert.True(result.IsAuthorized);
        Assert.True(result.IsEvaluation);
        Assert.Equal(30, result.DaysRemaining);
        Assert.NotNull(result.License);
        Assert.Equal("Trial", result.License.LicenseType);
        Assert.Contains("POS", result.License.AllowedModules);
        Assert.Contains("Restaurant", result.License.AllowedModules);
    }

    // 14. LicenseService integration when Trial Expired
    [Fact]
    public void LicenseService_ValidateCurrentLicense_ReturnsExpiredEvaluationWhenTrialExpires()
    {
        TrialStateManager.StartTrial(_testMachineId, _baseTime);
        var day35 = _baseTime.AddDays(35);
        TrialStateManager.SetTestingOverrides(_testDir, () => day35, _testMachineId);
        LicenseService.SetLicenseFilePathForTesting(null);

        var result = LicenseService.ValidateCurrentLicense();

        Assert.Equal(LicenseStatus.Expired, result.Status);
        Assert.False(result.IsAuthorized);
        Assert.True(result.IsEvaluation);
        Assert.Equal(0, result.DaysRemaining);
    }
}
