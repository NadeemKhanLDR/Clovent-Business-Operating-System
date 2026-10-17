using System.Collections.Concurrent;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Notifications;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Platform.CircuitBreakers;
using Clovent.Platform.Printing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Printing;

/// <summary>
/// Cohesive platform coordinator for discovering printer queues, managing logical profiles,
/// routing document roles, tracking job lifecycles, monitoring peripheral health,
/// isolating communication faults via circuit breakers, executing authorized test prints,
/// and automatically quarantining failed print jobs with instantaneous cashier fallback.
/// Enforces permissions at the service boundary.
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
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly IAuthorizationService? _authorizationService;
    private readonly ICurrentSession? _currentSession;
    private readonly ILogger<PrinterManagementService>? _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _dispatchLocks = new();

    /// <summary>
    /// Event raised instantaneously when a receipt print job is quarantined to notify the cashier,
    /// ensuring zero downtime and non-interruption of the completed payment tender.
    /// </summary>
    public event EventHandler<CashierReceiptFallbackNotification>? CashierFallbackNotified;

    /// <summary>Constructs the printer management coordinator service with peripheral resilience and optional authorization services.</summary>
    public PrinterManagementService(
        IWindowsPrinterQueueProvider queueProvider,
        IPrinterConfigurationStore configStore,
        IPrinterRouter router,
        IPrinterJobTracker jobTracker,
        IPrinterAdapter spoolerAdapter,
        IPrintJobQuarantineStore? quarantineStore = null,
        ICircuitBreakerRegistry? circuitBreakerRegistry = null,
        INotificationService? notificationService = null,
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
        _quarantineStore = quarantineStore ?? new PrintJobQuarantineStore(
            Path.Combine(Path.GetTempPath(), "cbos-ephemeral-quarantine", Guid.NewGuid().ToString("N")), null);
        _circuitBreakerRegistry = circuitBreakerRegistry ?? new CircuitBreakerRegistry();
        _notificationService = notificationService;
        _authorizationService = authorizationService;
        _currentSession = currentSession;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    /// <summary>Constructs the printer management coordinator service with authorization dependencies.</summary>
    public PrinterManagementService(
        IWindowsPrinterQueueProvider queueProvider,
        IPrinterConfigurationStore configStore,
        IPrinterRouter router,
        IPrinterJobTracker jobTracker,
        IPrinterAdapter spoolerAdapter,
        IAuthorizationService? authorizationService,
        ICurrentSession? currentSession = null,
        ILogger<PrinterManagementService>? logger = null,
        IServiceScopeFactory? scopeFactory = null)
        : this(
            queueProvider,
            configStore,
            router,
            jobTracker,
            spoolerAdapter,
            quarantineStore: null,
            circuitBreakerRegistry: null,
            notificationService: null,
            authorizationService: authorizationService,
            currentSession: currentSession,
            logger: logger,
            scopeFactory: scopeFactory)
    {
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

    /// <summary>
    /// Enforces strict company, branch, and terminal ownership boundaries.
    /// Invariant: Manager/Admin role status MUST NOT bypass scope ownership boundaries.
    /// </summary>
    public void EnsureScopeOwnership(Guid? organizationId, Guid? branchId, Guid? terminalId, string actionDescription)
    {
        PosSettingsStore.EnsureWorkstationScopeOwnership(organizationId, branchId, terminalId, actionDescription);
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

    /// <summary>Retrieves receipt copies and layout configuration for a specific terminal machine.</summary>
    public async Task<TerminalReceiptSettings> GetTerminalReceiptSettingsAsync(Guid? terminalId, CancellationToken cancellationToken = default)
    {
        EnsureScopeOwnership(organizationId: null, branchId: null, terminalId: terminalId, "reading terminal receipt settings");
        var config = await _configStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        return config.GetSettingsForTerminal(terminalId);
    }

    /// <summary>Saves receipt copies and layout configuration for a specific terminal machine.</summary>
    public async Task SaveTerminalReceiptSettingsAsync(
        Guid terminalId,
        TerminalReceiptSettings settings,
        Guid? organizationId = null,
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        await EnsurePermissionAsync("feature.printers.manage", "save terminal receipt settings").ConfigureAwait(false);
        EnsureScopeOwnership(organizationId: organizationId, branchId: branchId, terminalId: terminalId, "saving terminal receipt settings");

        var config = await _configStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        config.SetSettingsForTerminal(terminalId, settings);
        await _configStore.SaveAsync(config, cancellationToken).ConfigureAwait(false);
        _logger?.LogInformation("Saved receipt settings for terminal {TerminalId}", terminalId);
    }

    /// <summary>Sets a profile as the explicit default receipt printer for a specific terminal machine.</summary>
    public async Task AssignTerminalDefaultReceiptPrinterAsync(
        Guid terminalId,
        Guid profileId,
        Guid? organizationId = null,
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        await EnsurePermissionAsync("feature.printers.manage", "assign terminal default receipt printer").ConfigureAwait(false);
        EnsureScopeOwnership(organizationId: organizationId, branchId: branchId, terminalId: terminalId, "assigning terminal default receipt printer");

        var config = await _configStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var profile = config.FindProfile(profileId) ?? throw new InvalidOperationException($"Printer profile {profileId} not found.");

        // Remove any prior default receipt assignment for this terminal
        config.Assignments.RemoveAll(a => a.TerminalId == terminalId && a.Role == PrinterRole.Receipt);

        config.Assignments.Add(new PrinterAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            TerminalId = terminalId,
            Role = PrinterRole.Receipt,
            PrinterProfileId = profileId,
            IsDefaultReceiptPrinter = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _configStore.SaveAsync(config, cancellationToken).ConfigureAwait(false);
        _logger?.LogInformation("Assigned profile '{ProfileName}' as default receipt printer for terminal {TerminalId}", profile.ProfileName, terminalId);
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
        Guid? organizationId = null,
        Guid? branchId = null,
        Guid? terminalId = null,
        bool isReprint = false,
        int reprintCount = 1,
        string? reprintReason = null,
        Guid? jobId = null,
        ReceiptCopyType copyType = ReceiptCopyType.Customer,
        TerminalReceiptSettings? customSettings = null,
        Guid? targetProfileId = null,
        bool isAdditionalCopy = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        // Enforce company, branch, and terminal scope boundaries (Manager/Admin cannot bypass)
        EnsureScopeOwnership(organizationId, branchId, terminalId, "receipt dispatch");

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
            PrinterProfile? profile = null;
            if (targetProfileId.HasValue)
            {
                var config = await _configStore.LoadAsync(cancellationToken).ConfigureAwait(false);
                profile = config.FindProfile(targetProfileId.Value);
            }

            if (profile == null)
            {
                var routingResult = _router.ResolvePrinterDetailed(organizationId, branchId, terminalId, PrinterRole.Receipt);
                if (routingResult.Outcome != PrinterResolutionOutcome.Success || routingResult.Profile is not { } resolved)
                {
                    _logger?.LogWarning("Receipt printer resolution failed ({Outcome}): {Message}", routingResult.Outcome, routingResult.Message);
                    return PrintDispatchResult.Failed(null, routingResult.Message ?? "No receipt printer configured or assigned for this scope.");
                }
                profile = resolved;
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

            var effectiveSettings = customSettings ?? (await _configStore.LoadAsync(cancellationToken).ConfigureAwait(false)).GetSettingsForTerminal(terminalId);

            var renderOptions = new ReceiptRenderOptions(
                PaperWidth: effectiveSettings?.PaperWidth ?? profile.PaperWidth,
                CharactersPerLine: effectiveSettings?.CharactersPerLine ?? profile.CharactersPerLine,
                IsReprint: isReprint || copyType == ReceiptCopyType.Reprint,
                ReprintCount: reprintCount,
                ReprintReason: reprintReason,
                HeaderText: effectiveSettings?.HeaderText ?? "Clovent Business Operating System",
                FooterText: effectiveSettings?.FooterText ?? "Thank you for your visit!",
                CopyType: isReprint ? ReceiptCopyType.Reprint : copyType,
                MarginMm: effectiveSettings?.MarginMm ?? 2,
                LogoPath: effectiveSettings?.LogoPath,
                EnableLogo: effectiveSettings?.EnableLogo ?? false);

            var formatted = ReceiptSnapshotFormatter.Format(data, renderOptions);

            var docType = isReprint || copyType == ReceiptCopyType.Reprint
                ? "ReprintReceipt"
                : copyType switch
                {
                    ReceiptCopyType.Merchant => "MerchantReceipt",
                    ReceiptCopyType.Draft => "DraftBill",
                    _ => "CustomerReceipt"
                };

            var job = PrinterJob.Create(
                docType,
                profile.Id,
                profile.SystemPrinterName,
                formatted.FormattedText,
                organizationId,
                branchId,
                terminalId,
                isReprint || copyType == ReceiptCopyType.Reprint,
                reprintCount,
                reprintReason,
                documentReference: data.OrderNumber,
                jobId: effectiveJobId,
                documentId: data.OrderId,
                logoPath: effectiveSettings?.EnableLogo == true ? effectiveSettings.LogoPath : null,
                marginMm: effectiveSettings?.MarginMm ?? profile.MarginMm,
                paperWidth: effectiveSettings?.PaperWidth ?? profile.PaperWidth);

            // Cash drawer kick is STRICTLY permitted ONLY on the primary customer copy of cash tenders.
            // Reprints, additional customer copies (copy 2..N), merchant copies, drafts, and non-cash tenders NEVER trigger the drawer!
            var hasCashTender = data.Payments.Any(p => string.Equals(p.PaymentMethodName, "Cash", StringComparison.OrdinalIgnoreCase));
            var isCashDrawerAllowed = !isReprint
                && !isAdditionalCopy
                && copyType == ReceiptCopyType.Customer
                && hasCashTender
                && (effectiveSettings?.SupportsCashDrawer ?? profile.SupportsCashDrawer);

            var effectiveProfile = profile;
            if (profile.SupportsCashDrawer != isCashDrawerAllowed)
            {
                effectiveProfile = new PrinterProfile
                {
                    Id = profile.Id,
                    ProfileName = profile.ProfileName,
                    SystemPrinterName = profile.SystemPrinterName,
                    Role = profile.Role,
                    ConnectionType = profile.ConnectionType,
                    PaperWidth = effectiveSettings?.PaperWidth ?? profile.PaperWidth,
                    CharactersPerLine = effectiveSettings?.CharactersPerLine ?? profile.CharactersPerLine,
                    MarginMm = effectiveSettings?.MarginMm ?? profile.MarginMm,
                    PrintCopies = profile.PrintCopies,
                    SupportsCutter = effectiveSettings?.SupportsCutter ?? profile.SupportsCutter,
                    SupportsCashDrawer = isCashDrawerAllowed,
                    IsEnabled = profile.IsEnabled
                };
            }

            return await ExecuteResilientDispatchAsync(effectiveProfile, job, data.OrderNumber, cancellationToken).ConfigureAwait(false);
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
            // Enforce company, branch, and terminal scope boundaries (Manager/Admin cannot bypass)
            EnsureScopeOwnership(organizationId, branchId, terminalId, "text print dispatch");

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

            return await ExecuteResilientDispatchAsync(profile, job, documentReference ?? "Document", cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            sem.Release();
            _dispatchLocks.TryRemove(lockKey, out _);
        }
    }

    private async Task<PrintDispatchResult> ExecuteResilientDispatchAsync(
        PrinterProfile profile,
        PrinterJob job,
        string referenceLabel,
        CancellationToken cancellationToken)
    {
        _jobTracker.RecordJob(job);
        _jobTracker.MarkQueued(job.JobId);

        var circuitBreaker = GetCircuitBreaker(profile.SystemPrinterName);

        // Circuit Breaker Fast-Fail: Prevent thread pool exhaustion during physical hardware disconnects
        if (!circuitBreaker.CanExecute())
        {
            var openReason = $"Peripheral circuit breaker is OPEN for printer '{profile.SystemPrinterName}' " +
                             $"({circuitBreaker.LastExceptionMessage ?? "Device unreachable"}). Dispatch bypassed to prevent thread pool exhaustion.";
            _logger?.LogWarning(openReason);

            return await QuarantineAndNotifyAsync(job, profile, openReason, referenceLabel, cancellationToken).ConfigureAwait(false);
        }

        // Proactive hardware polling before dispatch
        var health = await _spoolerAdapter.CheckHealthAsync(profile, cancellationToken).ConfigureAwait(false);
        if (health.HasFault)
        {
            var faultReason = $"Printer '{profile.SystemPrinterName}' hardware fault detected: {health.Condition} - {health.StatusMessage}";
            _logger?.LogWarning(faultReason);

            circuitBreaker.RecordFailure(new InvalidOperationException(faultReason));
            return await QuarantineAndNotifyAsync(job, profile, faultReason, referenceLabel, cancellationToken).ConfigureAwait(false);
        }

        _jobTracker.MarkInFlight(job.JobId);

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

            return dispatchResult;
        }
        catch (OperationCanceledException)
        {
            _jobTracker.MarkAmbiguous(job.JobId, "Dispatch operation cancelled or timed out before spooler confirmation.");
            return PrintDispatchResult.Ambiguous(profile.SystemPrinterName, "Dispatch timed out; spooler acceptance uncertain.");
        }
        catch (Exception ex)
        {
            return await QuarantineAndNotifyAsync(job, profile, ex.Message, referenceLabel, cancellationToken).ConfigureAwait(false);
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
    /// Enforces authorization at the service boundary.
    /// </summary>
    public async Task<PrintDispatchResult> ExecuteTestPrintAsync(
        Guid profileId,
        string? operatorName = null,
        bool authorizedExplicitly = true,
        CancellationToken cancellationToken = default)
    {
        if (!authorizedExplicitly)
        {
            throw new InvalidOperationException("Physical test print execution requires explicit operator authorization.");
        }

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
