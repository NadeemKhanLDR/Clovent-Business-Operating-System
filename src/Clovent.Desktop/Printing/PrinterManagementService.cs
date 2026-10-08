using Clovent.Platform.Printing;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Printing;

/// <summary>
/// Cohesive platform coordinator for discovering printer queues, managing logical profiles,
/// routing document roles, tracking job lifecycles, and executing authorized test prints.
/// </summary>
public sealed class PrinterManagementService
{
    private readonly IWindowsPrinterQueueProvider _queueProvider;
    private readonly IPrinterConfigurationStore _configStore;
    private readonly IPrinterRouter _router;
    private readonly IPrinterJobTracker _jobTracker;
    private readonly IPrinterAdapter _spoolerAdapter;
    private readonly ILogger<PrinterManagementService>? _logger;

    /// <summary>Constructs the printer management coordinator service.</summary>
    public PrinterManagementService(
        IWindowsPrinterQueueProvider queueProvider,
        IPrinterConfigurationStore configStore,
        IPrinterRouter router,
        IPrinterJobTracker jobTracker,
        IPrinterAdapter spoolerAdapter,
        ILogger<PrinterManagementService>? logger = null)
    {
        _queueProvider = queueProvider ?? throw new ArgumentNullException(nameof(queueProvider));
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
        _router = router ?? throw new ArgumentNullException(nameof(router));
        _jobTracker = jobTracker ?? throw new ArgumentNullException(nameof(jobTracker));
        _spoolerAdapter = spoolerAdapter ?? throw new ArgumentNullException(nameof(spoolerAdapter));
        _logger = logger;
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

        try
        {
            var dispatchResult = await _spoolerAdapter.DispatchAsync(profile, job, cancellationToken);
            if (dispatchResult.Success)
            {
                _jobTracker.MarkSubmitted(job.JobId);
            }
            else
            {
                _jobTracker.MarkFailed(job.JobId, dispatchResult.ErrorMessage ?? "Unknown spooler error");
            }

            return dispatchResult;
        }
        catch (Exception ex)
        {
            _jobTracker.MarkFailed(job.JobId, ex.Message);
            return PrintDispatchResult.Failed(profile.SystemPrinterName, ex.Message);
        }
    }

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
