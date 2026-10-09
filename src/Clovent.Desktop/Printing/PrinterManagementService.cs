using System.Collections.Concurrent;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Platform.Printing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Printing;

/// <summary>
/// Cohesive platform coordinator for discovering printer queues, managing logical profiles,
/// routing document roles, tracking job lifecycles, and executing authorized test prints.
/// Enforces permissions at the service boundary.
/// </summary>
public sealed class PrinterManagementService
{
    private readonly IWindowsPrinterQueueProvider _queueProvider;
    private readonly IPrinterConfigurationStore _configStore;
    private readonly IPrinterRouter _router;
    private readonly IPrinterJobTracker _jobTracker;
    private readonly IPrinterAdapter _spoolerAdapter;
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly IAuthorizationService? _authorizationService;
    private readonly ICurrentSession? _currentSession;
    private readonly ILogger<PrinterManagementService>? _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _dispatchLocks = new();

    /// <summary>Constructs the printer management coordinator service.</summary>
    public PrinterManagementService(
        IWindowsPrinterQueueProvider queueProvider,
        IPrinterConfigurationStore configStore,
        IPrinterRouter router,
        IPrinterJobTracker jobTracker,
        IPrinterAdapter spoolerAdapter,
        IAuthorizationService? authorizationService = null,
        ICurrentSession? currentSession = null,
        ILogger<PrinterManagementService>? logger = null,
        IServiceScopeFactory? scopeFactory = null)
    {
        _queueProvider = queueProvider ?? throw new ArgumentNullException(nameof(queueProvider));
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
        _router = router ?? throw new ArgumentNullException(nameof(router));
        _jobTracker = jobTracker ?? throw new ArgumentNullException(nameof(jobTracker));
        _spoolerAdapter = spoolerAdapter ?? throw new ArgumentNullException(nameof(spoolerAdapter));
        _authorizationService = authorizationService;
        _currentSession = currentSession;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    private async Task EnsurePermissionAsync(string permissionCode, string actionDescription)
    {
        if (_scopeFactory == null && _authorizationService == null)
        {
            return;
        }

        if (_currentSession == null || !_currentSession.IsAuthenticated || _currentSession.UserId is not { } userId)
        {
            throw new UnauthorizedAccessException($"Access Denied: You must be authenticated to {actionDescription}.");
        }

        if (_scopeFactory != null)
        {
            using var scope = _scopeFactory.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
            var hasPermission = await authService.HasPermissionAsync(userId, permissionCode).ConfigureAwait(false);
            if (!hasPermission)
            {
                var isAdmin = await authService.HasRoleAsync(userId, "Administrator").ConfigureAwait(false);
                if (!isAdmin)
                {
                    throw new UnauthorizedAccessException($"Access Denied: Missing permission '{permissionCode}' required to {actionDescription}.");
                }
            }
            return;
        }

        if (_authorizationService != null)
        {
            var hasPermission = await _authorizationService.HasPermissionAsync(userId, permissionCode).ConfigureAwait(false);
            if (!hasPermission)
            {
                var isAdmin = await _authorizationService.HasRoleAsync(userId, "Administrator").ConfigureAwait(false);
                if (!isAdmin)
                {
                    throw new UnauthorizedAccessException($"Access Denied: Missing permission '{permissionCode}' required to {actionDescription}.");
                }
            }
        }
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
        await EnsurePermissionAsync("feature.printers.manage", "save printer profiles").ConfigureAwait(false);

        var config = await _configStore.LoadAsync().ConfigureAwait(false);
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

        await _configStore.SaveAsync(config).ConfigureAwait(false);
        _logger?.LogInformation("Saved printer profile {ProfileName} ({ProfileId})", profile.ProfileName, profile.Id);
    }

    /// <summary>Deletes a printer profile and any associated routing assignments.</summary>
    public async Task DeleteProfileAsync(Guid profileId)
    {
        await EnsurePermissionAsync("feature.printers.manage", "delete printer profiles").ConfigureAwait(false);

        var config = await _configStore.LoadAsync().ConfigureAwait(false);
        config.Profiles.RemoveAll(p => p.Id == profileId);
        config.Assignments.RemoveAll(a => a.PrinterProfileId == profileId);

        if (config.DefaultReceiptProfileId == profileId)
        {
            config.DefaultReceiptProfileId = config.Profiles.FirstOrDefault()?.Id;
        }

        await _configStore.SaveAsync(config).ConfigureAwait(false);
        _logger?.LogInformation("Deleted printer profile {ProfileId}", profileId);
    }

    /// <summary>Sets a profile as the global default receipt printer.</summary>
    public async Task SetDefaultReceiptPrinterAsync(Guid profileId)
    {
        await EnsurePermissionAsync("feature.printers.manage", "set default receipt printer").ConfigureAwait(false);

        var config = await _configStore.LoadAsync().ConfigureAwait(false);
        var profile = config.FindProfile(profileId);
        if (profile == null)
        {
            throw new InvalidOperationException($"Printer profile {profileId} not found.");
        }

        config.DefaultReceiptProfileId = profileId;
        await _configStore.SaveAsync(config).ConfigureAwait(false);
    }

    /// <summary>Assigns a printer profile to a specific scope (organization, branch, terminal, role).</summary>
    public async Task AssignPrinterAsync(PrinterAssignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        await EnsurePermissionAsync("feature.printers.manage", "assign printer scopes").ConfigureAwait(false);

        var config = await _configStore.LoadAsync().ConfigureAwait(false);
        config.Assignments.RemoveAll(a => a.Id == assignment.Id);
        config.Assignments.Add(assignment);

        await _configStore.SaveAsync(config).ConfigureAwait(false);
    }

    /// <summary>Removes a scope assignment.</summary>
    public async Task RemoveAssignmentAsync(Guid assignmentId)
    {
        await EnsurePermissionAsync("feature.printers.manage", "remove printer assignments").ConfigureAwait(false);

        var config = await _configStore.LoadAsync().ConfigureAwait(false);
        config.Assignments.RemoveAll(a => a.Id == assignmentId);
        await _configStore.SaveAsync(config).ConfigureAwait(false);
    }

    /// <summary>
    /// Dispatches a customer receipt print job to the appropriate printer profile
    /// determined by the execution scope.
    /// </summary>
    public async Task<PrintDispatchResult> DispatchReceiptAsync(
        PrintableReceiptData data,
        Guid? organizationId,
        Guid? branchId,
        Guid? terminalId,
        bool isReprint = false,
        int reprintCount = 1,
        string? reprintReason = null,
        Guid? jobId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        var effectiveJobId = jobId ?? Guid.NewGuid();
        var lockKey = $"receipt:{effectiveJobId}";
        var sem = _dispatchLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
        var lockAcquired = await sem.WaitAsync(0, cancellationToken).ConfigureAwait(false);
        if (!lockAcquired)
        {
            _logger?.LogWarning("Concurrent dispatch already in progress for job {JobId} ({OrderNumber}); suppressing duplicate dispatch.", effectiveJobId, data.OrderNumber);
            return PrintDispatchResult.Ambiguous(null, "Concurrent dispatch already in progress for this job; duplicate submission suppressed.");
        }

        try
        {
            var routingResult = _router.ResolvePrinterDetailed(organizationId, branchId, terminalId, PrinterRole.Receipt);
            if (routingResult.Outcome != PrinterResolutionOutcome.Success || routingResult.Profile is not { } profile)
            {
                _logger?.LogWarning("Receipt printer resolution failed ({Outcome}): {Message}", routingResult.Outcome, routingResult.Message);
                return PrintDispatchResult.Failed(null, routingResult.Message ?? "No receipt printer configured or assigned for this scope.");
            }

            if (!_queueProvider.IsPrinterInstalled(profile.SystemPrinterName))
            {
                _logger?.LogWarning("Target print queue '{Queue}' is not installed in Windows.", profile.SystemPrinterName);
                return PrintDispatchResult.Failed(profile.SystemPrinterName, $"Target Windows print queue '{profile.SystemPrinterName}' is not installed or unavailable.");
            }

            // Idempotency check: A retry after spooler acceptance or in-flight dispatch cannot create a fresh duplicate job.
            if (_jobTracker.GetJob(effectiveJobId) is { } existingJob)
            {
                if (existingJob.Status is PrinterJobStatus.Submitted or PrinterJobStatus.Confirmed)
                {
                    _logger?.LogInformation("Print job {JobId} for {Ref} was already submitted to spooler. Suppressing duplicate job.", effectiveJobId, data.OrderNumber);
                    return existingJob.Status == PrinterJobStatus.Confirmed
                        ? PrintDispatchResult.Confirmed(existingJob.TargetSystemPrinterName ?? profile.SystemPrinterName)
                        : PrintDispatchResult.SpoolerAccepted(existingJob.TargetSystemPrinterName ?? profile.SystemPrinterName);
                }

                if (existingJob.Status is PrinterJobStatus.InFlight or PrinterJobStatus.Ambiguous)
                {
                    _logger?.LogWarning("Print job {JobId} for {Ref} is in {Status} state. Suppressing duplicate job.", effectiveJobId, data.OrderNumber, existingJob.Status);
                    return PrintDispatchResult.Ambiguous(existingJob.TargetSystemPrinterName ?? profile.SystemPrinterName, $"Print job execution is in {existingJob.Status} state; automatic reprint suppressed.");
                }
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
                reprintReason,
                documentReference: data.OrderNumber,
                jobId: effectiveJobId,
                documentId: data.OrderId);

            _jobTracker.RecordJob(job);
            _jobTracker.MarkQueued(job.JobId);
            _jobTracker.MarkInFlight(job.JobId);

            try
            {
                var dispatchResult = await _spoolerAdapter.DispatchAsync(profile, job, cancellationToken).ConfigureAwait(false);
                if (dispatchResult.Success)
                {
                    try
                    {
                        _jobTracker.MarkSubmitted(job.JobId);
                    }
                    catch (Exception persistEx)
                    {
                        _logger?.LogError(persistEx, "Failed to persist submitted status for job {JobId}; marking as ambiguous", job.JobId);
                        try { _jobTracker.MarkAmbiguous(job.JobId, "Spooler accepted job but state persistence failed."); } catch { }
                        return PrintDispatchResult.Ambiguous(profile.SystemPrinterName, "Print job accepted by spooler but status persistence failed.");
                    }
                }
                else
                {
                    _jobTracker.MarkFailed(job.JobId, dispatchResult.ErrorMessage ?? "Unknown spooler error");
                }

                return dispatchResult;
            }
            catch (OperationCanceledException)
            {
                _jobTracker.MarkAmbiguous(job.JobId, "Dispatch operation cancelled or timed out before spooler confirmation.");
                return PrintDispatchResult.Ambiguous(profile.SystemPrinterName, "Dispatch timed out; spooler acceptance uncertain.");
            }
            catch (Exception ex)
            {
                _jobTracker.MarkAmbiguous(job.JobId, $"Dispatch encountered unexpected error: {ex.Message}. Status is ambiguous.");
                return PrintDispatchResult.Ambiguous(profile.SystemPrinterName, $"Dispatch error: {ex.Message}. Spooler status uncertain.");
            }
        }
        finally
        {
            sem.Release();
            _dispatchLocks.TryRemove(lockKey, out _);
        }
    }

    /// <summary>
    /// Dispatches a pre-formatted plain-text print job to the appropriate printer profile
    /// determined by the execution scope.
    /// </summary>
    public async Task<PrintDispatchResult> DispatchTextAsync(
        string rawText,
        string documentType = "CustomerReceipt",
        string? documentReference = null,
        Guid? organizationId = null,
        Guid? branchId = null,
        Guid? terminalId = null,
        bool isReprint = false,
        int reprintCount = 1,
        string? reprintReason = null,
        Guid? jobId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawText);

        var effectiveJobId = jobId ?? Guid.NewGuid();
        var lockKey = $"text:{effectiveJobId}";
        var sem = _dispatchLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
        var lockAcquired = await sem.WaitAsync(0, cancellationToken).ConfigureAwait(false);
        if (!lockAcquired)
        {
            _logger?.LogWarning("Concurrent dispatch already in progress for job {JobId} ({Ref}); suppressing duplicate dispatch.", effectiveJobId, documentReference);
            return PrintDispatchResult.Ambiguous(null, "Concurrent dispatch already in progress for this job; duplicate submission suppressed.");
        }

        try
        {
            var routingResult = _router.ResolvePrinterDetailed(organizationId, branchId, terminalId, PrinterRole.Receipt);
            if (routingResult.Outcome != PrinterResolutionOutcome.Success || routingResult.Profile is not { } profile)
            {
                return PrintDispatchResult.Failed(null, routingResult.Message ?? "No receipt printer configured or assigned for this scope.");
            }

            if (!_queueProvider.IsPrinterInstalled(profile.SystemPrinterName))
            {
                return PrintDispatchResult.Failed(profile.SystemPrinterName, $"Target Windows print queue '{profile.SystemPrinterName}' is not installed or unavailable.");
            }

            // Idempotency check: A retry after spooler acceptance or in-flight dispatch cannot create a fresh duplicate job.
            if (_jobTracker.GetJob(effectiveJobId) is { } existingJob)
            {
                if (existingJob.Status is PrinterJobStatus.Submitted or PrinterJobStatus.Confirmed)
                {
                    _logger?.LogInformation("Print job {JobId} for {Ref} was already submitted to spooler. Suppressing duplicate job.", effectiveJobId, documentReference);
                    return existingJob.Status == PrinterJobStatus.Confirmed
                        ? PrintDispatchResult.Confirmed(existingJob.TargetSystemPrinterName ?? profile.SystemPrinterName)
                        : PrintDispatchResult.SpoolerAccepted(existingJob.TargetSystemPrinterName ?? profile.SystemPrinterName);
                }

                if (existingJob.Status is PrinterJobStatus.InFlight or PrinterJobStatus.Ambiguous)
                {
                    _logger?.LogWarning("Print job {JobId} for {Ref} is in {Status} state. Suppressing duplicate job.", effectiveJobId, documentReference, existingJob.Status);
                    return PrintDispatchResult.Ambiguous(existingJob.TargetSystemPrinterName ?? profile.SystemPrinterName, $"Print job execution is in {existingJob.Status} state; automatic reprint suppressed.");
                }
            }

            var job = PrinterJob.Create(
                documentType,
                profile.Id,
                profile.SystemPrinterName,
                rawText,
                organizationId,
                branchId,
                terminalId,
                isReprint,
                reprintCount,
                reprintReason,
                documentReference: documentReference,
                jobId: effectiveJobId);

            _jobTracker.RecordJob(job);
            _jobTracker.MarkQueued(job.JobId);
            _jobTracker.MarkInFlight(job.JobId);

            try
            {
                var dispatchResult = await _spoolerAdapter.DispatchAsync(profile, job, cancellationToken).ConfigureAwait(false);
                if (dispatchResult.Success)
                {
                    try
                    {
                        _jobTracker.MarkSubmitted(job.JobId);
                    }
                    catch (Exception persistEx)
                    {
                        _logger?.LogError(persistEx, "Failed to persist submitted status for job {JobId}; marking as ambiguous", job.JobId);
                        try { _jobTracker.MarkAmbiguous(job.JobId, "Spooler accepted job but state persistence failed."); } catch { }
                        return PrintDispatchResult.Ambiguous(profile.SystemPrinterName, "Print job accepted by spooler but status persistence failed.");
                    }
                }
                else
                {
                    _jobTracker.MarkFailed(job.JobId, dispatchResult.ErrorMessage ?? "Unknown spooler error");
                }

                return dispatchResult;
            }
            catch (OperationCanceledException)
            {
                _jobTracker.MarkAmbiguous(job.JobId, "Dispatch operation cancelled or timed out before spooler confirmation.");
                return PrintDispatchResult.Ambiguous(profile.SystemPrinterName, "Dispatch timed out; spooler acceptance uncertain.");
            }
            catch (Exception ex)
            {
                _jobTracker.MarkAmbiguous(job.JobId, $"Dispatch encountered unexpected error: {ex.Message}. Status is ambiguous.");
                return PrintDispatchResult.Ambiguous(profile.SystemPrinterName, $"Dispatch error: {ex.Message}. Spooler status uncertain.");
            }
        }
        finally
        {
            sem.Release();
            _dispatchLocks.TryRemove(lockKey, out _);
        }
    }

    /// <summary>
    /// Executes an explicit diagnostic test print to the specified profile.
    /// Enforces authorization at the service boundary.
    /// </summary>
    public async Task<PrintDispatchResult> ExecuteTestPrintAsync(
        Guid profileId,
        string? operatorName = null,
        CancellationToken cancellationToken = default)
    {
        await EnsurePermissionAsync("feature.printers.test", "execute hardware test prints").ConfigureAwait(false);

        var config = await _configStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var profile = config.FindProfile(profileId);
        if (profile == null)
        {
            return PrintDispatchResult.Failed(null, $"Printer profile {profileId} not found.");
        }

        if (!profile.IsEnabled)
        {
            return PrintDispatchResult.Failed(profile.SystemPrinterName, $"Printer profile '{profile.ProfileName}' is disabled.");
        }

        if (!_queueProvider.IsPrinterInstalled(profile.SystemPrinterName))
        {
            return PrintDispatchResult.Failed(profile.SystemPrinterName, $"Target Windows print queue '{profile.SystemPrinterName}' is not installed or unavailable.");
        }

        var effectiveOperator = !string.IsNullOrWhiteSpace(operatorName)
            ? operatorName
            : (_currentSession?.DisplayName ?? "Administrator");

        var testSlipText = ReceiptSnapshotFormatter.FormatTestPrintSlip(profile, effectiveOperator);

        var job = PrinterJob.Create(
            "TestPrint",
            profile.Id,
            profile.SystemPrinterName,
            testSlipText,
            documentReference: "DIAGNOSTIC-TEST");

        _jobTracker.RecordJob(job);
        _jobTracker.MarkQueued(job.JobId);

        try
        {
            var result = await _spoolerAdapter.DispatchAsync(profile, job, cancellationToken).ConfigureAwait(false);
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
        catch (OperationCanceledException)
        {
            _jobTracker.MarkAmbiguous(job.JobId, "Dispatch operation cancelled or timed out before spooler confirmation.");
            return PrintDispatchResult.Ambiguous(profile.SystemPrinterName, "Dispatch timed out; spooler acceptance uncertain.");
        }
        catch (Exception ex)
        {
            _jobTracker.MarkFailed(job.JobId, ex.Message);
            return PrintDispatchResult.Failed(profile.SystemPrinterName, ex.Message);
        }
    }
}
