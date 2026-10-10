using Clovent.Desktop.Notifications;
using Clovent.Platform.CircuitBreakers;
using Clovent.Platform.Printing;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Printing;

/// <summary>
/// Cohesive platform coordinator for discovering printer queues, managing logical profiles,
/// routing document roles, tracking job lifecycles, monitoring peripheral health,
/// isolating communication faults via circuit breakers, executing authorized test prints,
/// and automatically quarantining failed print jobs with instantaneous cashier fallback.
/// </summary>
public sealed class PrinterManagementService
{
    private readonly IWindowsPrinterQueueProvider _queueProvider;
    private readonly IPrinterConfigurationStore _configStore;
    private readonly IPrinterRouter _router;
    private readonly IPrinterJobTracker _jobTracker;
    private readonly IPrinterAdapter _spoolerAdapter;
    private readonly IPrintJobQuarantineStore _quarantineStore;
    private readonly ICircuitBreakerRegistry _circuitBreakerRegistry;
    private readonly INotificationService? _notificationService;
    private readonly ILogger<PrinterManagementService>? _logger;

    /// <summary>
    /// Event raised instantaneously when a receipt print job is quarantined to notify the cashier,
    /// ensuring zero downtime and non-interruption of the completed payment tender.
    /// </summary>
    public event EventHandler<CashierReceiptFallbackNotification>? CashierFallbackNotified;

    /// <summary>Constructs the printer management coordinator service.</summary>
    public PrinterManagementService(
        IWindowsPrinterQueueProvider queueProvider,
        IPrinterConfigurationStore configStore,
        IPrinterRouter router,
        IPrinterJobTracker jobTracker,
        IPrinterAdapter spoolerAdapter,
        IPrintJobQuarantineStore? quarantineStore = null,
        ICircuitBreakerRegistry? circuitBreakerRegistry = null,
        INotificationService? notificationService = null,
        ILogger<PrinterManagementService>? logger = null)
    {
        _queueProvider = queueProvider ?? throw new ArgumentNullException(nameof(queueProvider));
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
        _router = router ?? throw new ArgumentNullException(nameof(router));
        _jobTracker = jobTracker ?? throw new ArgumentNullException(nameof(jobTracker));
        _spoolerAdapter = spoolerAdapter ?? throw new ArgumentNullException(nameof(spoolerAdapter));
        _quarantineStore = quarantineStore ?? new PrintJobQuarantineStore(null, null);
        _circuitBreakerRegistry = circuitBreakerRegistry ?? new CircuitBreakerRegistry();
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>Resolves or creates the circuit breaker protecting the specified printer queue.</summary>
    public ICircuitBreaker GetCircuitBreaker(string printerName)
    {
        var breakerName = $"Printer:{printerName}";
        return _circuitBreakerRegistry.GetOrCreate(breakerName, new CircuitBreakerOptions
        {
            FailureThreshold = 3,
            OpenDuration = TimeSpan.FromSeconds(30),
            HalfOpenSuccessThreshold = 1
        });
    }

    /// <summary>Resets the circuit breaker for the specified printer queue.</summary>
    public void ResetCircuitBreaker(string printerName)
    {
        GetCircuitBreaker(printerName).Reset();
        _logger?.LogInformation("Reset circuit breaker for printer queue '{PrinterName}'", printerName);
    }

    /// <summary>Lists all physical and virtual print queues installed in Windows.</summary>
    public Task<IReadOnlyList<WindowsPrinterInfo>> GetInstalledPrintersAsync() =>
        Task.FromResult(_queueProvider.GetInstalledPrinters());

    /// <summary>Retrieves the active printer configuration.</summary>
    public Task<PrinterConfiguration> GetConfigurationAsync() =>
        _configStore.LoadAsync();

    /// <summary>Saves or updates a logical printer profile.</summary>
    public async Task SaveProfileAsync(PrinterProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var config = await _configStore.LoadAsync();
        var existing = config.FindProfile(profile.Id);

        if (existing != null)
        {
            config.Profiles.Remove(existing);
        }

        profile.UpdatedAtUtc = DateTimeOffset.UtcNow;
        config.Profiles.Add(profile);

        // If this is the only profile, set as default
        if (config.Profiles.Count == 1)
        {
            config.DefaultReceiptProfileId = profile.Id;
        }

        await _configStore.SaveAsync(config);
        _logger?.LogInformation("Saved printer profile {ProfileName} ({ProfileId})", profile.ProfileName, profile.Id);
    }

    /// <summary>Deletes a printer profile and any associated routing assignments.</summary>
    public async Task DeleteProfileAsync(Guid profileId)
    {
        var config = await _configStore.LoadAsync();
        config.Profiles.RemoveAll(p => p.Id == profileId);
        config.Assignments.RemoveAll(a => a.PrinterProfileId == profileId);

        if (config.DefaultReceiptProfileId == profileId)
        {
            config.DefaultReceiptProfileId = config.Profiles.FirstOrDefault()?.Id;
        }

        await _configStore.SaveAsync(config);
        _logger?.LogInformation("Deleted printer profile {ProfileId}", profileId);
    }

    /// <summary>Sets a profile as the global default receipt printer.</summary>
    public async Task SetDefaultReceiptPrinterAsync(Guid profileId)
    {
        var config = await _configStore.LoadAsync();
        var profile = config.FindProfile(profileId);
        if (profile == null)
        {
            throw new InvalidOperationException($"Printer profile {profileId} not found.");
        }

        config.DefaultReceiptProfileId = profileId;
        await _configStore.SaveAsync(config);
    }

    /// <summary>Assigns a printer profile to a specific scope (organization, branch, terminal, role).</summary>
    public async Task AssignPrinterAsync(PrinterAssignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        var config = await _configStore.LoadAsync();
        config.Assignments.RemoveAll(a => a.Id == assignment.Id);
        config.Assignments.Add(assignment);

        await _configStore.SaveAsync(config);
    }

    /// <summary>Removes a scope assignment.</summary>
    public async Task RemoveAssignmentAsync(Guid assignmentId)
    {
        var config = await _configStore.LoadAsync();
        config.Assignments.RemoveAll(a => a.Id == assignmentId);
        await _configStore.SaveAsync(config);
    }

    /// <summary>
    /// Polls physical hardware status for the given profile.
    /// </summary>
    public async Task<PrinterHealthSnapshot> GetPrinterHealthAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        var config = await _configStore.LoadAsync(cancellationToken);
        var profile = config.FindProfile(profileId);
        if (profile == null)
        {
            return PrinterHealthSnapshot.Faulted("Unknown", PrinterHardwareCondition.Offline, "Profile not found", profileId);
        }

        return await _spoolerAdapter.CheckHealthAsync(profile, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Polls physical hardware status across all configured printer profiles.
    /// </summary>
    public async Task<IReadOnlyList<PrinterHealthSnapshot>> GetAllPrintersHealthAsync(
        CancellationToken cancellationToken = default)
    {
        var config = await _configStore.LoadAsync(cancellationToken);
        var snapshots = new List<PrinterHealthSnapshot>();

        foreach (var profile in config.Profiles)
        {
            try
            {
                var snapshot = await _spoolerAdapter.CheckHealthAsync(profile, cancellationToken).ConfigureAwait(false);
                snapshots.Add(snapshot);
            }
            catch (Exception ex)
            {
                snapshots.Add(PrinterHealthSnapshot.Faulted(
                    profile.SystemPrinterName,
                    PrinterHardwareCondition.GeneralError,
                    ex.Message,
                    profile.Id));
            }
        }

        return snapshots;
    }

    /// <summary>
    /// Dispatches a customer receipt print job to the appropriate printer profile.
    /// Incorporates pre-flight hardware status polling, circuit breaker fault isolation,
    /// and automatic quarantine with cashier notification on failure to ensure completed sales
    /// and payment tenders are never aborted.
    /// </summary>
    public async Task<PrintDispatchResult> DispatchReceiptAsync(
        PrintableReceiptData data,
        Guid? organizationId,
        Guid? branchId,
        Guid? terminalId,
        bool isReprint = false,
        int reprintCount = 1,
        string? reprintReason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        var profile = _router.ResolvePrinter(organizationId, branchId, terminalId, PrinterRole.Receipt);
        if (profile == null)
        {
            _logger?.LogWarning("No receipt printer profile resolved for scope Org:{Org} Branch:{Branch} Terminal:{Terminal}",
                organizationId, branchId, terminalId);
            return PrintDispatchResult.Failed(null, "No receipt printer configured or assigned for this terminal.");
        }

        var renderOptions = new ReceiptRenderOptions(
            PaperWidth: profile.PaperWidth,
            CharactersPerLine: profile.CharactersPerLine,
            IsReprint: isReprint,
            ReprintCount: reprintCount,
            ReprintReason: reprintReason);

        var formatted = ReceiptSnapshotFormatter.Format(data, renderOptions);

        var docType = isReprint ? "ReprintReceipt" : "CustomerReceipt";
        var job = PrinterJob.Create(
            docType,
            profile.Id,
            profile.SystemPrinterName,
            formatted.FormattedText,
            organizationId,
            branchId,
            terminalId,
            isReprint,
            reprintCount,
            reprintReason);

        _jobTracker.RecordJob(job);
        _jobTracker.MarkQueued(job.JobId);

        var circuitBreaker = GetCircuitBreaker(profile.SystemPrinterName);

        // Circuit Breaker Fast-Fail: Prevent thread pool exhaustion during physical hardware disconnects
        if (!circuitBreaker.CanExecute())
        {
            var openReason = $"Peripheral circuit breaker is OPEN for printer '{profile.SystemPrinterName}' " +
                             $"({circuitBreaker.LastExceptionMessage ?? "Device unreachable"}). Dispatch bypassed to prevent thread pool exhaustion.";
            _logger?.LogWarning(openReason);

            return await QuarantineAndNotifyAsync(job, profile, openReason, data.OrderNumber, cancellationToken).ConfigureAwait(false);
        }

        // Proactive hardware polling before dispatch
        var health = await _spoolerAdapter.CheckHealthAsync(profile, cancellationToken).ConfigureAwait(false);
        if (health.HasFault)
        {
            var faultReason = $"Printer '{profile.SystemPrinterName}' hardware fault detected: {health.Condition} - {health.StatusMessage}";
            _logger?.LogWarning(faultReason);

            circuitBreaker.RecordFailure(new InvalidOperationException(faultReason));
            return await QuarantineAndNotifyAsync(job, profile, faultReason, data.OrderNumber, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var dispatchResult = await circuitBreaker.ExecuteAsync(async () =>
            {
                var result = await _spoolerAdapter.DispatchAsync(profile, job, cancellationToken).ConfigureAwait(false);
                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorMessage ?? "Windows print spooler rejected job.");
                }
                return result;
            }, cancellationToken).ConfigureAwait(false);

            _jobTracker.MarkSubmitted(job.JobId);
            return dispatchResult;
        }
        catch (Exception ex)
        {
            return await QuarantineAndNotifyAsync(job, profile, ex.Message, data.OrderNumber, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<PrintDispatchResult> QuarantineAndNotifyAsync(
        PrinterJob job,
        PrinterProfile profile,
        string failureReason,
        string orderNumber,
        CancellationToken cancellationToken)
    {
        _jobTracker.MarkQuarantined(job.JobId, failureReason);

        var quarantinedJob = new QuarantinedPrintJob
        {
            JobId = job.JobId,
            CorrelationId = job.CorrelationId,
            DocumentType = job.DocumentType,
            TargetPrinterProfileId = profile.Id,
            TargetSystemPrinterName = profile.SystemPrinterName,
            PayloadText = job.PayloadText,
            FailureReason = failureReason,
            QuarantinedAtUtc = DateTimeOffset.UtcNow,
            OrganizationId = job.OrganizationId,
            BranchId = job.BranchId,
            TerminalId = job.TerminalId,
            IsReprint = job.IsReprint,
            ReprintCount = job.ReprintCount,
            ReprintReason = job.ReprintReason
        };

        try
        {
            await _quarantineStore.QuarantineJobAsync(quarantinedJob, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to persist quarantined print job {JobId} to disk", job.JobId);
        }

        var notification = new CashierReceiptFallbackNotification(
            job.JobId,
            job.CorrelationId.ToString(),
            profile.SystemPrinterName,
            failureReason,
            DateTimeOffset.UtcNow);

        try
        {
            CashierFallbackNotified?.Invoke(this, notification);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error executing CashierFallbackNotified event handler");
        }

        _notificationService?.Add(
            "Receipt Printing Fallback",
            $"Receipt for order {orderNumber} diverted to quarantine ({failureReason}). Sale completed successfully.");

        return PrintDispatchResult.Quarantined(profile.SystemPrinterName, failureReason);
    }

    /// <summary>
    /// Re-dispatches a single quarantined print job to its target printer queue.
    /// If successful, removes the job from the quarantine buffer.
    /// </summary>
    public async Task<PrintDispatchResult> ReplayQuarantinedJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var quarantinedJob = await _quarantineStore.GetJobAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (quarantinedJob == null)
        {
            return PrintDispatchResult.Failed(null, $"Quarantined job {jobId} not found.");
        }

        var config = await _configStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var profile = config.FindProfile(quarantinedJob.TargetPrinterProfileId)
                      ?? config.Profiles.FirstOrDefault(p => string.Equals(p.SystemPrinterName, quarantinedJob.TargetSystemPrinterName, StringComparison.OrdinalIgnoreCase));

        if (profile == null)
        {
            return PrintDispatchResult.Failed(quarantinedJob.TargetSystemPrinterName,
                $"Target printer profile for '{quarantinedJob.TargetSystemPrinterName}' no longer exists.");
        }

        // Verify device is ready before retrying
        var health = await _spoolerAdapter.CheckHealthAsync(profile, cancellationToken).ConfigureAwait(false);
        if (health.HasFault)
        {
            quarantinedJob.RetryCount++;
            quarantinedJob.LastRetryAtUtc = DateTimeOffset.UtcNow;
            quarantinedJob.LastRetryError = health.StatusMessage;
            await _quarantineStore.UpdateJobAsync(quarantinedJob, cancellationToken).ConfigureAwait(false);

            return PrintDispatchResult.Failed(profile.SystemPrinterName,
                $"Printer hardware still in fault state: {health.Condition} - {health.StatusMessage}");
        }

        var job = PrinterJob.Create(
            quarantinedJob.DocumentType,
            profile.Id,
            profile.SystemPrinterName,
            quarantinedJob.PayloadText,
            quarantinedJob.OrganizationId,
            quarantinedJob.BranchId,
            quarantinedJob.TerminalId,
            quarantinedJob.IsReprint,
            quarantinedJob.ReprintCount,
            quarantinedJob.ReprintReason,
            quarantinedJob.CorrelationId);

        try
        {
            var dispatchResult = await _spoolerAdapter.DispatchAsync(profile, job, cancellationToken).ConfigureAwait(false);
            if (dispatchResult.Success)
            {
                await _quarantineStore.DeleteJobAsync(jobId, cancellationToken).ConfigureAwait(false);
                _jobTracker.MarkSubmitted(job.JobId);
                _logger?.LogInformation("Successfully re-spooled quarantined job {JobId} to '{Printer}'", jobId, profile.SystemPrinterName);
                return dispatchResult;
            }
            else
            {
                quarantinedJob.RetryCount++;
                quarantinedJob.LastRetryAtUtc = DateTimeOffset.UtcNow;
                quarantinedJob.LastRetryError = dispatchResult.ErrorMessage;
                await _quarantineStore.UpdateJobAsync(quarantinedJob, cancellationToken).ConfigureAwait(false);
                return dispatchResult;
            }
        }
        catch (Exception ex)
        {
            quarantinedJob.RetryCount++;
            quarantinedJob.LastRetryAtUtc = DateTimeOffset.UtcNow;
            quarantinedJob.LastRetryError = ex.Message;
            await _quarantineStore.UpdateJobAsync(quarantinedJob, cancellationToken).ConfigureAwait(false);
            return PrintDispatchResult.Failed(profile.SystemPrinterName, ex.Message);
        }
    }

    /// <summary>
    /// Retries re-dispatching all quarantined print jobs currently buffered on disk.
    /// </summary>
    public async Task<QuarantineReplaySummary> ReplayAllQuarantinedJobsAsync(
        CancellationToken cancellationToken = default)
    {
        var quarantinedJobs = await _quarantineStore.GetQuarantinedJobsAsync(cancellationToken).ConfigureAwait(false);
        int successCount = 0;
        int failureCount = 0;

        foreach (var job in quarantinedJobs)
        {
            var result = await ReplayQuarantinedJobAsync(job.JobId, cancellationToken).ConfigureAwait(false);
            if (result.Success)
            {
                successCount++;
            }
            else
            {
                failureCount++;
            }
        }

        return new QuarantineReplaySummary(quarantinedJobs.Count, successCount, failureCount);
    }

    /// <summary>Deletes a quarantined job from storage without printing.</summary>
    public Task<bool> DeleteQuarantinedJobAsync(Guid jobId, CancellationToken cancellationToken = default) =>
        _quarantineStore.DeleteJobAsync(jobId, cancellationToken);

    /// <summary>Lists all currently quarantined print jobs.</summary>
    public Task<IReadOnlyList<QuarantinedPrintJob>> GetQuarantinedJobsAsync(CancellationToken cancellationToken = default) =>
        _quarantineStore.GetQuarantinedJobsAsync(cancellationToken);

    /// <summary>Gets the current count of quarantined print jobs on disk.</summary>
    public Task<int> GetQuarantinedJobCountAsync(CancellationToken cancellationToken = default) =>
        _quarantineStore.GetQuarantinedJobCountAsync(cancellationToken);

    /// <summary>
    /// Executes an explicit diagnostic test print to the specified profile.
    /// Strictly requires explicit user authorization before actuating hardware.
    /// </summary>
    public async Task<PrintDispatchResult> ExecuteTestPrintAsync(
        Guid profileId,
        string operatorName,
        bool authorizedExplicitly,
        CancellationToken cancellationToken = default)
    {
        if (!authorizedExplicitly)
        {
            throw new InvalidOperationException("Test print requires explicit operator confirmation before actuating hardware.");
        }

        var config = await _configStore.LoadAsync();
        var profile = config.FindProfile(profileId);
        if (profile == null)
        {
            return PrintDispatchResult.Failed(null, $"Printer profile {profileId} not found.");
        }

        var testSlipText = ReceiptSnapshotFormatter.FormatTestPrintSlip(profile, operatorName);

        var job = PrinterJob.Create(
            "TestPrint",
            profile.Id,
            profile.SystemPrinterName,
            testSlipText);

        _jobTracker.RecordJob(job);
        _jobTracker.MarkQueued(job.JobId);

        try
        {
            var result = await _spoolerAdapter.DispatchAsync(profile, job, cancellationToken);
            if (result.Success)
            {
                _jobTracker.MarkSubmitted(job.JobId);
            }
            else
            {
                _jobTracker.MarkFailed(job.JobId, result.ErrorMessage ?? "Test print dispatch failed.");
            }

            return result;
        }
        catch (Exception ex)
        {
            _jobTracker.MarkFailed(job.JobId, ex.Message);
            return PrintDispatchResult.Failed(profile.SystemPrinterName, ex.Message);
        }
    }
}
