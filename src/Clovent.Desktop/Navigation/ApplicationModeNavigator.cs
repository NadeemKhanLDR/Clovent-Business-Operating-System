using System;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Identity;
using Clovent.Desktop.Forms.Shell;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Desktop.Sessions;
using Clovent.Desktop.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Navigation;

/// <summary>
/// Single source of truth for switching the desktop application between
/// Restaurant POS and Back Office shell modes. Owns the single-window transition
/// lifecycle: creates the destination window, sets it as the active main window,
/// disposes the previous window, and ensures no duplicate or orphaned windows remain.
/// </summary>
public sealed class ApplicationModeNavigator : IApplicationModeNavigator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CbosApplicationContext _appContext;
    private readonly IErrorDialogService _errorDialogService;
    private readonly ILogger<ApplicationModeNavigator> _logger;

    private Form? _currentForm;
    private IWorkspaceHost? _currentWorkspaceHost;
    private bool _isTransitioning;

    private SynchronizationContext? _uiSyncContext;
    private readonly int _uiThreadId;

    public ApplicationModeNavigator(
        IServiceProvider serviceProvider,
        CbosApplicationContext appContext,
        IErrorDialogService errorDialogService,
        ILogger<ApplicationModeNavigator> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _appContext = appContext ?? throw new ArgumentNullException(nameof(appContext));
        _errorDialogService = errorDialogService ?? throw new ArgumentNullException(nameof(errorDialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _uiThreadId = Environment.CurrentManagedThreadId;
        _uiSyncContext = SynchronizationContext.Current;
    }

    /// <inheritdoc/>
    public void SetUiSynchronizationContext(SynchronizationContext syncContext)
    {
        if (syncContext is not null)
        {
            _uiSyncContext = syncContext;
        }
    }

    private SynchronizationContext? ResolveUiSyncContext()
    {
        if (_uiSyncContext is not null)
        {
            return _uiSyncContext;
        }

        if (SynchronizationContext.Current is not null)
        {
            _uiSyncContext = SynchronizationContext.Current;
            return _uiSyncContext;
        }

        return _uiSyncContext;
    }

    private Task MarshalToUiThreadAsync(Func<Task> action)
    {
        var syncContext = ResolveUiSyncContext();
        if (syncContext is not null)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            syncContext.Post(async _ =>
            {
                try
                {
                    await action().ConfigureAwait(false);
                    tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, null);
            return tcs.Task;
        }

        if (_currentForm is not null && _currentForm.IsHandleCreated && !_currentForm.IsDisposed)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _currentForm.BeginInvoke(new Action(async () =>
            {
                try
                {
                    await action().ConfigureAwait(false);
                    tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }));
            return tcs.Task;
        }

        if (Application.OpenForms.Count > 0)
        {
            var openForm = Application.OpenForms[0];
            if (openForm is not null && openForm.IsHandleCreated && !openForm.IsDisposed)
            {
                var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                openForm.BeginInvoke(new Action(async () =>
                {
                    try
                    {
                        await action().ConfigureAwait(false);
                        tcs.TrySetResult();
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                }));
                return tcs.Task;
            }
        }

        _logger.LogError("CRITICAL_THREAD_ERROR: Failed to marshal navigation to UI thread (UI Thread ID: {UiThreadId}, Current Thread ID: {CurrentThreadId}).", _uiThreadId, Environment.CurrentManagedThreadId);
        return action();
    }

    /// <inheritdoc/>
    public Form? CurrentForm => _currentForm;

    /// <inheritdoc/>
    public IWorkspaceHost? CurrentWorkspaceHost => _currentWorkspaceHost;

    /// <inheritdoc/>
    public bool IsTransitioning => _isTransitioning;

    /// <inheritdoc/>
    public ApplicationContext ApplicationContext => _appContext;

    /// <inheritdoc/>
    public Task OpenPosAsync(Clovent.Restaurant.Application.Shifts.Dtos.ShiftDto? activeShift = null)
    {
        if (Environment.CurrentManagedThreadId != _uiThreadId)
        {
            return MarshalToUiThreadAsync(() => OpenPosCoreAsync(activeShift));
        }

        return OpenPosCoreAsync(activeShift);
    }

    private Task OpenPosCoreAsync(Clovent.Restaurant.Application.Shifts.Dtos.ShiftDto? activeShift = null)
    {
        if (_isTransitioning)
        {
            _logger.LogWarning("Navigation transition already in progress; rejecting reentrant OpenPos request.");
            return Task.CompletedTask;
        }

        _isTransitioning = true;
        _appContext.IsTransitioning = true;
        _logger.LogInformation("NAVIGATOR_MODE_SWITCH_START: Switching mode from {From} to Restaurant POS on Thread {ThreadId} (UI Thread={UiThreadId})...",
            _currentForm?.GetType().Name ?? "None",
            Environment.CurrentManagedThreadId,
            _uiThreadId);

        RestaurantPosForm? freshPosForm = null;
        try
        {
            freshPosForm = _serviceProvider.GetRequiredService<RestaurantPosForm>();
            if (activeShift is not null)
            {
                freshPosForm.SetActiveShift(activeShift);
            }
        }
        catch (Exception ex)
        {
            _isTransitioning = false;
            _appContext.IsTransitioning = false;
            _logger.LogError(ex, "NAVIGATOR_MODE_SWITCH_FAILED: Failed to instantiate Restaurant POS form during mode switch.");
            _errorDialogService.ShowError(ex, "Failed to open Restaurant POS");
            return Task.CompletedTask;
        }

        var previousForm = _currentForm;
        _currentForm = freshPosForm;
        _currentWorkspaceHost = null;

        _appContext.SetActiveForm(freshPosForm);

        freshPosForm.Show();
        freshPosForm.Activate();

        if (previousForm is not null && !previousForm.IsDisposed)
        {
            try
            {
                previousForm.Close();
                previousForm.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception occurred while disposing previous main form during OpenPos transition.");
            }
        }

        _isTransitioning = false;
        _appContext.IsTransitioning = false;
        _logger.LogInformation("NAVIGATOR_MODE_SWITCH_SUCCESS: Successfully switched application mode to Restaurant POS.");

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OpenBackOfficeAsync(string? initialViewKey = "dashboard", string? initialCaption = "Dashboard")
    {
        if (Environment.CurrentManagedThreadId != _uiThreadId)
        {
            return MarshalToUiThreadAsync(() => OpenBackOfficeCoreAsync(initialViewKey, initialCaption));
        }

        return OpenBackOfficeCoreAsync(initialViewKey, initialCaption);
    }

    private Task OpenBackOfficeCoreAsync(string? initialViewKey = "dashboard", string? initialCaption = "Dashboard")
    {
        if (_isTransitioning)
        {
            _logger.LogWarning("Navigation transition already in progress; rejecting reentrant OpenBackOffice request.");
            return Task.CompletedTask;
        }

        _isTransitioning = true;
        _appContext.IsTransitioning = true;
        _logger.LogInformation("NAVIGATOR_MODE_SWITCH_START: Switching mode from {From} to Back Office on Thread {ThreadId} (UI Thread={UiThreadId})...",
            _currentForm?.GetType().Name ?? "None",
            Environment.CurrentManagedThreadId,
            _uiThreadId);

        MainForm? freshMainForm = null;
        try
        {
            freshMainForm = _serviceProvider.GetRequiredService<MainForm>();
            _currentWorkspaceHost = freshMainForm;

            var navService = _serviceProvider.GetRequiredService<INavigationService>();
            var viewKey = string.IsNullOrWhiteSpace(initialViewKey) ? "dashboard" : initialViewKey;
            var caption = string.IsNullOrWhiteSpace(initialCaption) ? "Dashboard" : initialCaption;
            navService.NavigateTo(viewKey, caption);
        }
        catch (Exception ex)
        {
            _isTransitioning = false;
            _appContext.IsTransitioning = false;
            _currentWorkspaceHost = _currentForm as IWorkspaceHost;
            _logger.LogError(ex, "NAVIGATOR_MODE_SWITCH_FAILED: Failed to instantiate Back Office shell form during mode switch.");
            _errorDialogService.ShowError(ex, "Failed to open Back Office");
            return Task.CompletedTask;
        }

        var previousForm = _currentForm;
        _currentForm = freshMainForm;

        _appContext.SetActiveForm(freshMainForm);

        freshMainForm.Show();
        freshMainForm.Activate();

        if (previousForm is not null && !previousForm.IsDisposed)
        {
            try
            {
                previousForm.Close();
                previousForm.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception occurred while disposing previous main form during OpenBackOffice transition.");
            }
        }

        _isTransitioning = false;
        _appContext.IsTransitioning = false;
        _logger.LogInformation("NAVIGATOR_MODE_SWITCH_SUCCESS: Successfully switched application mode to Back Office.");

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OpenLoginAsync()
    {
        if (Environment.CurrentManagedThreadId != _uiThreadId)
        {
            return MarshalToUiThreadAsync(OpenLoginCoreAsync);
        }

        return OpenLoginCoreAsync();
    }

    private Task OpenLoginCoreAsync()
    {
        if (_isTransitioning)
        {
            _logger.LogWarning("Navigation transition already in progress; rejecting reentrant OpenLogin request.");
            return Task.CompletedTask;
        }

        _isTransitioning = true;
        _appContext.IsTransitioning = true;
        _logger.LogInformation("NAVIGATOR_MODE_SWITCH_START: Switching mode from {From} to Login on Thread {ThreadId} (UI Thread={UiThreadId})...",
            _currentForm?.GetType().Name ?? "None",
            Environment.CurrentManagedThreadId,
            _uiThreadId);

        // Sign out in-memory user session (attendance record in database is NOT punched out)
        var session = _serviceProvider.GetService<ICurrentSession>();
        session?.SignOut();

        LoginForm? freshLoginForm = null;
        try
        {
            freshLoginForm = _serviceProvider.GetRequiredService<LoginForm>();
        }
        catch (Exception ex)
        {
            _isTransitioning = false;
            _appContext.IsTransitioning = false;
            _logger.LogError(ex, "NAVIGATOR_MODE_SWITCH_FAILED: Failed to instantiate LoginForm during OpenLogin transition.");
            _errorDialogService.ShowError(ex, "Failed to open Login screen");
            return Task.CompletedTask;
        }

        var previousForm = _currentForm;
        _currentForm = freshLoginForm;
        _currentWorkspaceHost = null;

        _appContext.SetActiveForm(freshLoginForm);

        if (previousForm is not null && !previousForm.IsDisposed)
        {
            try
            {
                previousForm.Close();
                previousForm.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception occurred while disposing previous main form during OpenLogin transition.");
            }
        }

        freshLoginForm.FormClosing += (s, e) =>
        {
            if (freshLoginForm.DialogResult == DialogResult.OK && !string.IsNullOrWhiteSpace(freshLoginForm.SelectedModuleKey))
            {
                _isTransitioning = true;
                _appContext.IsTransitioning = true;
            }
        };

        freshLoginForm.FormClosed += async (s, e) =>
        {
            if (freshLoginForm.DialogResult == DialogResult.OK && !string.IsNullOrWhiteSpace(freshLoginForm.SelectedModuleKey))
            {
                var targetModule = freshLoginForm.SelectedModuleKey;
                _isTransitioning = false;
                _appContext.IsTransitioning = false;

                try
                {
                    if (string.Equals(targetModule, "pos", StringComparison.OrdinalIgnoreCase))
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var gate = scope.ServiceProvider.GetService<Clovent.Desktop.Restaurant.Services.IPosEntryGateCoordinator>();
                        var opened = gate != null && await gate.EnsureShiftAndOpenPosAsync().ConfigureAwait(false);
                        if (!opened)
                        {
                            await OpenBackOfficeAsync().ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await OpenBackOfficeAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Navigation following login encountered an error.");
                    _errorDialogService.ShowError(ex, "Navigation failed");
                }
            }
            else
            {
                _isTransitioning = false;
                _appContext.IsTransitioning = false;
                ExitApplication("LoginCancelled");
            }
        };

        freshLoginForm.Show();
        freshLoginForm.Activate();

        _isTransitioning = false;
        _appContext.IsTransitioning = false;
        _logger.LogInformation("NAVIGATOR_MODE_SWITCH_SUCCESS: Successfully switched application mode to Login screen.");

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void ExitApplication() => ExitApplication("User");

    /// <inheritdoc/>
    public void ExitApplication(string initiator)
    {
        _logger.LogInformation("APPLICATION_EXIT_REQUESTED: Initiator={Initiator}", initiator);
        _isTransitioning = false;
        _appContext.IsTransitioning = false;

        if (_currentForm is not null && !_currentForm.IsDisposed)
        {
            _currentForm.Close();
            _currentForm.Dispose();
            _currentForm = null;
        }

        _appContext.Exit();
        _logger.LogInformation("Application exit invoked via mode navigator.");
    }
}
