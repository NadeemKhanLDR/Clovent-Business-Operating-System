using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.ActivityLogs.Commands;
using Clovent.Restaurant.Application.SmartRecommendations.Commands;
using Clovent.Restaurant.Application.SmartRecommendations.Dtos;
using Clovent.Restaurant.Application.SmartRecommendations.Queries;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Restaurant.SmartPos;

/// <summary>
/// Recommendation Rules Management View: lists the Smart POS basket-
/// recommendation rules (active and inactive), supports create/edit via
/// <see cref="RecommendationRuleEditForm"/> and activate/deactivate (the soft
/// delete). Visual Studio Designer compatible.
/// </summary>
public sealed partial class RecommendationRulesView : XtraUserControl
{
    private const string FeatureCode = "recommendationrules";

    private readonly IServiceScope _scope;
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly ILogger<RecommendationRulesView> _logger;

    private List<RecommendationRuleDto> _allRules = [];
    private Dictionary<Guid, string> _productNamesByVariantId = [];
    private Dictionary<Guid, string> _productNamesByProductId = [];
    private List<ProductOptionRow> _variantOptions = [];
    private bool _isLoading;

    /// <summary>Builds the screen and starts its own DI scope.</summary>
    public RecommendationRulesView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();

        // One scope-shared gate around the mediator and feature policy - the
        // same single-DbContext serialization every other Restaurant screen
        // uses (defect D22); see SerializedMediator's doc comment.
        _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
        _featurePolicy = new SerializedFeatureAuthorizationPolicy(
            _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>(), _gate);
        _logger = _scope.ServiceProvider.GetRequiredService<ILogger<RecommendationRulesView>>();
        _currentSession = currentSession;

        InitializeComponent();
        ScaleLayoutAtRuntime();
    }

    /// <summary>Design-time-only constructor for Visual Studio Designer.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public RecommendationRulesView()
    {
        _scope = null!;
        _mediator = null!;
        _featurePolicy = null!;
        _logger = null!;
        _currentSession = null!;

        InitializeComponent();
        ScaleLayoutAtRuntime();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope?.Dispose();
            _gate?.Dispose();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private Task TryRunAsync(Func<Task> action, string actionDescription) =>
        GuardedAction.RunAsync(this, _logger, action, actionDescription);

    /// <summary>Formats a day-of-week bitmask as a readable caption (bit <c>d</c> = <see cref="DayOfWeek"/> value <c>d</c>).</summary>
    public static string FormatDays(int? daysOfWeek)
    {
        if (daysOfWeek is null || daysOfWeek.Value == 0b0111_1111)
        {
            return "Every day";
        }

        var names = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        var selected = Enumerable.Range(0, 7)
            .Where(d => (daysOfWeek.Value & (1 << d)) != 0)
            .Select(d => names[d])
            .ToList();
        return selected.Count == 0 ? "No days" : string.Join(", ", selected);
    }

    private static string FormatTimeWindow(TimeSpan? start, TimeSpan? end) =>
        start is { } s && end is { } e ? $"{s:hh\\:mm}-{e:hh\\:mm}" : "-";

    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        Cursor = Cursors.WaitCursor;
        try
        {
            var rules = await _mediator.Send(new ListRecommendationRulesQuery(), cancellationToken);
            _allRules = [.. rules];
            _gridControl.DataSource = _allRules
                .Select(r => new RuleGridRow(r, _productNamesByProductId, _productNamesByVariantId))
                .ToList();
            _gridView.BestFitColumns();

            _lblSummary.Text = $"Rules: {_allRules.Count}   |   Active: {_allRules.Count(r => r.IsActive)}";
            await UpdateActionButtonsStateAsync();
        }
        finally
        {
            _isLoading = false;
            Cursor = Cursors.Default;
        }
    }

    /// <summary>Loads the catalog option list the edit dialog's pickers bind to (rule ids alone carry no product names).</summary>
    private async Task LoadCatalogOptionsAsync(CancellationToken cancellationToken = default)
    {
        _variantOptions = [.. await SmartPosCatalogOptions.LoadAsync(_mediator, cancellationToken)];
        _productNamesByVariantId = _variantOptions.ToDictionary(o => o.VariantId, o => o.ProductName);
        _productNamesByProductId = _variantOptions
            .GroupBy(o => o.ProductId)
            .ToDictionary(g => g.Key, g => g.First().ProductName);
    }

    private async Task UpdateActionButtonsStateAsync()
    {
        if (DesignModeHelper.IsInDesignMode || _currentSession.UserId is not { } userId)
        {
            return;
        }

        var focused = GetFocusedRule();
        var hasSelection = focused is not null;

        _newButton.Enabled = await _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.create");
        var canEdit = hasSelection && await _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.edit");
        _btnEdit.Enabled = canEdit;
        _btnToggleStatus.Enabled = hasSelection && await _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.deactivate");
        _btnToggleStatus.Text = focused is { IsActive: true } ? "Deactivate" : "Activate";
    }

    private RecommendationRuleDto? GetFocusedRule() =>
        _gridView.GetFocusedRow() is RuleGridRow row ? ruleById(row.RuleId) : null;

    private RecommendationRuleDto? ruleById(Guid ruleId) => _allRules.FirstOrDefault(r => r.RuleId == ruleId);

    private async Task LogActivityAsync(string action, string? details = null)
    {
        try
        {
            await _mediator.Send(new RecordActivityCommand(action, details, _currentSession.DisplayName ?? "Unknown", Environment.MachineName));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Swallowed per auditing guidelines
        }
    }

    // --- EVENT HANDLERS ---

    private async void RecommendationRulesView_Load(object? sender, EventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode)
            return;

        AppearanceManager.Apply(this, "Restaurant", nameof(RecommendationRulesView));
        await TryRunAsync(async () =>
        {
            await LoadCatalogOptionsAsync();
            await RefreshAsync();
        }, "load the recommendation rules");
    }

    private async void RefreshButton_Click(object? sender, EventArgs e) =>
        await TryRunAsync(() => RefreshAsync(), "refresh the recommendation rules");

    private async void NewButton_Click(object? sender, EventArgs e) => await EditRuleAsync(null);

    private async void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (GetFocusedRule() is { } rule)
        {
            await EditRuleAsync(rule);
        }
    }

    private bool _isEditDialogOpen;

    private async Task EditRuleAsync(RecommendationRuleDto? existing)
    {
        if (_isEditDialogOpen)
        {
            return;
        }

        _isEditDialogOpen = true;
        try
        {
            if (_currentSession.UserId is not { } userId || !await _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{(existing is null ? "create" : "edit")}"))
            {
                XtraMessageBox.Show(this, "You do not have permission to manage recommendation rules.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using var form = new RecommendationRuleEditForm(
                existing is null ? "New Recommendation Rule" : "Edit Recommendation Rule",
                _variantOptions,
                SmartPosCatalogOptions.ToProductSummaries(_variantOptions),
                existing?.ProductId,
                existing?.RecommendedVariantId ?? Guid.Empty,
                existing?.Priority ?? 1,
                existing?.IsActive ?? true,
                existing?.StartTime,
                existing?.EndTime,
                existing?.DaysOfWeek,
                existing?.Notes);

            if (form.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            if (existing is null)
            {
                await _mediator.Send(new CreateRecommendationRuleCommand(
                    form.TriggerProductIdValue,
                    form.RecommendedVariantIdValue,
                    form.PriorityValue,
                    form.StartTimeValue,
                    form.EndTimeValue,
                    form.DaysOfWeekValue,
                    form.NotesValue));
            }
            else
            {
                await _mediator.Send(new UpdateRecommendationRuleCommand(
                    existing.RuleId,
                    form.TriggerProductIdValue,
                    form.RecommendedVariantIdValue,
                    form.PriorityValue,
                    form.StartTimeValue,
                    form.EndTimeValue,
                    form.DaysOfWeekValue,
                    form.NotesValue));
            }

            await LogActivityAsync(existing is null ? "Recommendation Rule Created" : "Recommendation Rule Edited", form.NotesValue);
            await RefreshAsync();
        }
        finally
        {
            _isEditDialogOpen = false;
        }
    }

    private async void BtnToggleStatus_Click(object? sender, EventArgs e)
    {
        if (GetFocusedRule() is not { } rule)
        {
            return;
        }

        if (_currentSession.UserId is not { } userId || !await _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.deactivate"))
        {
            XtraMessageBox.Show(this, "You do not have permission to change recommendation rule status.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var targetActive = !rule.IsActive;
        var actionText = targetActive ? "Activate" : "Deactivate";
        if (XtraMessageBox.Show(this, $"Are you sure you want to {actionText.ToLower()} this recommendation rule?", $"Confirm {actionText}", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        await _mediator.Send(new SetRecommendationRuleStatusCommand(rule.RuleId, targetActive));
        await LogActivityAsync($"Recommendation Rule {actionText}d");
        await RefreshAsync();
    }

    private void BtnOrderHealth_Click(object? sender, EventArgs e)
    {
        using var dialog = new OrderHealthSettingsForm();
        dialog.ShowDialog(this);
    }

    private async void GridView_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e) =>
        await TryRunAsync(UpdateActionButtonsStateAsync, "update the toolbar");

    private async void GridView_RowCellClick(object sender, RowCellClickEventArgs e)
    {
        if (e.Clicks == 2 && GetFocusedRule() is { } rule)
        {
            await EditRuleAsync(rule);
        }
    }

    private void ScaleLayoutAtRuntime()
    {
        if (DesignModeHelper.IsInDesignMode) return;

        root.RowStyles[0] = new RowStyle(SizeType.AutoSize);
        root.RowStyles[2] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(36));

        _newButton.MinimumSize = LogicalToDeviceUnits(new Size(110, 32));
        _btnEdit.MinimumSize = LogicalToDeviceUnits(new Size(80, 32));
        _btnToggleStatus.MinimumSize = LogicalToDeviceUnits(new Size(100, 32));
        _btnOrderHealth.MinimumSize = LogicalToDeviceUnits(new Size(110, 32));
        _refreshButton.MinimumSize = LogicalToDeviceUnits(new Size(80, 32));

        _gridView.RowHeight = LogicalToDeviceUnits(30);
        _gridView.ColumnPanelRowHeight = LogicalToDeviceUnits(36);
    }

    // --- GRID VIEW ROW SHAPE ---

    private sealed class RuleGridRow(
        RecommendationRuleDto dto,
        IReadOnlyDictionary<Guid, string> productNameByProductId,
        IReadOnlyDictionary<Guid, string> productNameByVariantId)
    {
        public Guid RuleId => dto.RuleId;
        public string TriggerProduct => dto.ProductId is { } productId
            ? productNameByProductId.GetValueOrDefault(productId, "(unknown product)")
            : "Any Basket";
        public string RecommendedProduct => productNameByVariantId.GetValueOrDefault(dto.RecommendedVariantId, "(unknown product)");
        public int Priority => dto.Priority;
        public string StatusText => dto.IsActive ? "Active" : "Inactive";
        public string TimeWindow => FormatTimeWindow(dto.StartTime, dto.EndTime);
        public string Days => FormatDays(dto.DaysOfWeek);
        public string Notes => dto.Notes ?? "-";
    }
}
