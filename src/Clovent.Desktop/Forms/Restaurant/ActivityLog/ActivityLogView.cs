using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.ActivityLogs.Dtos;
using Clovent.Restaurant.Application.ActivityLogs.Queries;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Forms.Restaurant.ActivityLog;

/// <summary>
/// Read-only viewer over the Restaurant activity log (see
/// <c>Clovent.Restaurant.ActivityLogs.ActivityLogEntry</c>) - Date/Time,
/// User, Machine, Action, Details, newest first. No create/edit/activate
/// actions apply to a log, so this is a plain grid + search + Refresh rather
/// than <c>MasterDataListView&lt;TDto&gt;</c>'s CRUD shell. Feature-gated per
/// <c>activitylog.view</c>. Control tree lives in
/// <c>ActivityLogView.Designer.cs</c>; this file holds behavior only.
/// </summary>
public sealed partial class ActivityLogView : DevExpress.XtraEditors.XtraUserControl
{
    private const string FeatureCode = "activitylog";

    private readonly IServiceScope? _scope;
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    private IReadOnlyList<ActivityLogEntryDto> _allEntries = [];

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public ActivityLogView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        InitializeComponent();

        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
        {
            _mediator = null!;
            _featurePolicy = null!;
            _currentSession = null!;
            return;
        }

        _scope = scopeFactory.CreateScope();
        _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
        _featurePolicy = new SerializedFeatureAuthorizationPolicy(_scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>(), _gate);
        _currentSession = currentSession;
    }

    /// <summary>
    /// Design-time-only constructor - required for the Visual Studio
    /// WinForms Designer to host this control: the Designer instantiates
    /// the type being designed via a public parameterless constructor and
    /// never starts this application's DI container, so it cannot supply
    /// the constructor above's dependencies.
    /// </summary>
    public ActivityLogView() : this(null!, null!)
    {
    }

    private void AppearanceManager_Changed(object? sender, EventArgs e) => AppearanceManager.Apply(this, "Restaurant", nameof(ActivityLogView));

    private void GridView_CustomColumnDisplayText(object? sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
    {
        if (e.Column.FieldName == nameof(ActivityLogEntryDto.OccurredAtUtc) && e.Value is DateTimeOffset occurredAtUtc)
        {
            // Read in the viewer's local time, not raw UTC - the same
            // reasoning EndOfDayReportView's timestamp columns already
            // apply (RestaurantPOSArchitecture.md Section 15.1): a
            // restaurant owner reading an activity log shouldn't have to
            // mentally convert time zones.
            e.DisplayText = occurredAtUtc.ToLocalTime().ToString("g");
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            AppearanceManager.Changed -= AppearanceManager_Changed;
            _scope?.Dispose();
            _gate.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void SearchEdit_EditValueChanged(object? sender, EventArgs e) => ApplyFilter();

    private async void RefreshButton_Click(object? sender, EventArgs e) => await RefreshAsync();

    private async void ActivityLogView_Load(object? sender, EventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        AppearanceManager.Apply(this, "Restaurant", nameof(ActivityLogView));

        if (_currentSession.UserId is { } userId && !await _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.view"))
        {
            _allEntries = [];
            ApplyFilter();
            return;
        }

        _allEntries = [.. await _mediator.Send(new ListRecentActivityQuery())];
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var searchText = _searchEdit.Text.Trim();
        var filtered = string.IsNullOrEmpty(searchText)
            ? _allEntries
            : _allEntries.Where(e =>
                e.Action.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                e.PerformedBy.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (e.Details?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

        _grid.DataSource = filtered;
        _emptyStateLabel.Visible = filtered.Count == 0;
        if (_emptyStateLabel.Visible)
        {
            _emptyStateLabel.BringToFront();
        }
    }
}
