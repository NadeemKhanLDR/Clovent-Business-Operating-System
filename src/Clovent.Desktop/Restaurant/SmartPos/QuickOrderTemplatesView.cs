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
using Clovent.Restaurant.Application.QuickOrderTemplates.Commands;
using Clovent.Restaurant.Application.QuickOrderTemplates.Dtos;
using Clovent.Restaurant.Application.QuickOrderTemplates.Queries;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Restaurant.SmartPos;

/// <summary>
/// Quick Order Templates Management View: lists every template (active and
/// inactive) with its item count and resolved total, supports create/edit via
/// <see cref="QuickOrderTemplateEditForm"/> and activate/deactivate. Visual
/// Studio Designer compatible.
/// </summary>
public sealed partial class QuickOrderTemplatesView : XtraUserControl
{
    private const string FeatureCode = "quickordertemplates";

    private readonly IServiceScope _scope;
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly ILogger<QuickOrderTemplatesView> _logger;

    private List<QuickOrderTemplateDto> _allTemplates = [];
    private List<ProductOptionRow> _variantOptions = [];
    private bool _isLoading;
    private bool _isEditDialogOpen;

    /// <summary>Builds the screen and starts its own DI scope.</summary>
    public QuickOrderTemplatesView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();

        // One scope-shared gate around the mediator and feature policy - the
        // same single-DbContext serialization every other Restaurant screen
        // uses (defect D22); see SerializedMediator's doc comment.
        _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
        _featurePolicy = new SerializedFeatureAuthorizationPolicy(
            _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>(), _gate);
        _logger = _scope.ServiceProvider.GetRequiredService<ILogger<QuickOrderTemplatesView>>();
        _currentSession = currentSession;

        InitializeComponent();
        ScaleLayoutAtRuntime();
    }

    /// <summary>Design-time-only constructor for Visual Studio Designer.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public QuickOrderTemplatesView()
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
            var templates = await _mediator.Send(new ListAllQuickOrderTemplatesQuery(), cancellationToken);
            _allTemplates = [.. templates];
            _gridControl.DataSource = _allTemplates.Select(t => new TemplateGridRow(t)).ToList();
            _gridView.BestFitColumns();

            _lblSummary.Text =
                $"Templates: {_allTemplates.Count}   |   Active: {_allTemplates.Count(t => t.IsActive)}   |   " +
                $"Total Value: {CurrencyDisplay.FormatPlain(_allTemplates.Sum(t => t.TotalPrice))}";
            await UpdateActionButtonsStateAsync();
        }
        finally
        {
            _isLoading = false;
            Cursor = Cursors.Default;
        }
    }

    private async Task LoadCatalogOptionsAsync(CancellationToken cancellationToken = default)
    {
        _variantOptions = [.. await SmartPosCatalogOptions.LoadAsync(_mediator, cancellationToken)];
    }

    private async Task UpdateActionButtonsStateAsync()
    {
        if (DesignModeHelper.IsInDesignMode || _currentSession.UserId is not { } userId)
        {
            return;
        }

        var focused = GetFocusedTemplate();
        var hasSelection = focused is not null;

        _newButton.Enabled = await _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.create");
        _btnEdit.Enabled = hasSelection && await _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.edit");
        _btnToggleStatus.Enabled = hasSelection && await _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.deactivate");
        _btnToggleStatus.Text = focused is { IsActive: true } ? "Deactivate" : "Activate";
    }

    private QuickOrderTemplateDto? GetFocusedTemplate() =>
        _gridView.GetFocusedRow() is TemplateGridRow row
            ? _allTemplates.FirstOrDefault(t => t.TemplateId == row.TemplateId)
            : null;

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

    private async void QuickOrderTemplatesView_Load(object? sender, EventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode)
            return;

        AppearanceManager.Apply(this, "Restaurant", nameof(QuickOrderTemplatesView));
        await TryRunAsync(async () =>
        {
            await LoadCatalogOptionsAsync();
            await RefreshAsync();
        }, "load the quick order templates");
    }

    private async void RefreshButton_Click(object? sender, EventArgs e) =>
        await TryRunAsync(() => RefreshAsync(), "refresh the quick order templates");

    private async void NewButton_Click(object? sender, EventArgs e) => await CreateNewTemplateAsync();

    private async void BtnEdit_Click(object? sender, EventArgs e) => await OpenSelectedTemplateForEditAsync();

    public async Task OpenSelectedTemplateForEditAsync()
    {
        if (GetFocusedTemplate() is { } template)
        {
            await ShowEditDialogAsync(template);
        }
    }

    public async Task CreateNewTemplateAsync()
    {
        await ShowEditDialogAsync(null);
    }

    private async Task ShowEditDialogAsync(QuickOrderTemplateDto? existing)
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
                XtraMessageBox.Show(this, "You do not have permission to manage quick order templates.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using var form = new QuickOrderTemplateEditForm(
                existing is null ? "New Quick Order Template" : "Edit Quick Order Template",
                _variantOptions,
                existing is null
                    ? null
                    : new QuickOrderTemplateEditModel(
                        existing.Name,
                        existing.Description,
                        existing.DisplayOrder,
                        [.. existing.Items.Select(i => (i.VariantId, i.Quantity, i.TemplateUnitPrice ?? i.UnitPrice as decimal?))],
                        existing.IsActive));

            if (form.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var items = form.ItemValues
                .Select(v => new QuickOrderTemplateItemInput(v.VariantId, v.Quantity, v.TemplateUnitPrice))
                .ToList();

            if (existing is null)
            {
                var id = await _mediator.Send(new CreateQuickOrderTemplateCommand(form.NameValue, form.DescriptionValue, form.DisplayOrderValue, items));
                if (!form.IsActiveValue)
                {
                    await _mediator.Send(new SetQuickOrderTemplateStatusCommand(id, false));
                }
            }
            else
            {
                await _mediator.Send(new UpdateQuickOrderTemplateCommand(existing.TemplateId, form.NameValue, form.DescriptionValue, form.DisplayOrderValue, items));
                if (form.IsActiveValue != existing.IsActive)
                {
                    await _mediator.Send(new SetQuickOrderTemplateStatusCommand(existing.TemplateId, form.IsActiveValue));
                }
            }

            await LogActivityAsync(existing is null ? "Quick Order Template Created" : "Quick Order Template Edited", form.NameValue);
            await RefreshAsync();
        }
        finally
        {
            _isEditDialogOpen = false;
        }
    }

    private async void BtnToggleStatus_Click(object? sender, EventArgs e)
    {
        if (GetFocusedTemplate() is not { } template)
        {
            return;
        }

        if (_currentSession.UserId is not { } userId || !await _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.deactivate"))
        {
            XtraMessageBox.Show(this, "You do not have permission to change quick order template status.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var targetActive = !template.IsActive;
        var actionText = targetActive ? "Activate" : "Deactivate";
        if (XtraMessageBox.Show(this, $"Are you sure you want to {actionText.ToLower()} template '{template.Name}'?", $"Confirm {actionText}", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        await _mediator.Send(new SetQuickOrderTemplateStatusCommand(template.TemplateId, targetActive));
        await LogActivityAsync($"Quick Order Template {actionText}d", template.Name);
        await RefreshAsync();
    }

    private void BtnOrderHealth_Click(object? sender, EventArgs e)
    {
        using var dialog = new OrderHealthSettingsForm();
        dialog.ShowDialog(this);
    }

    private async void GridView_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e) =>
        await TryRunAsync(UpdateActionButtonsStateAsync, "update the toolbar");

    private async void GridView_DoubleClick(object? sender, EventArgs e)
    {
        var ea = e as DevExpress.Utils.DXMouseEventArgs;
        var pt = ea?.Location ?? _gridControl.PointToClient(Control.MousePosition);
        var info = _gridView.CalcHitInfo(pt);
        if (info != null && info.InRow && info.RowHandle >= 0)
        {
            await OpenSelectedTemplateForEditAsync();
        }
    }

    private void ScaleLayoutAtRuntime()
    {
        if (DesignModeHelper.IsInDesignMode) return;

        root.RowStyles[0] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(72));
        root.RowStyles[2] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(40));

        _newButton.MinimumSize = LogicalToDeviceUnits(new Size(130, 32));
        _btnEdit.MinimumSize = LogicalToDeviceUnits(new Size(80, 32));
        _btnToggleStatus.MinimumSize = LogicalToDeviceUnits(new Size(95, 32));
        _btnOrderHealth.MinimumSize = LogicalToDeviceUnits(new Size(110, 32));
        _refreshButton.MinimumSize = LogicalToDeviceUnits(new Size(80, 32));

        _gridView.RowHeight = LogicalToDeviceUnits(32);
        _gridView.ColumnPanelRowHeight = LogicalToDeviceUnits(36);
    }

    // --- GRID VIEW ROW SHAPE ---

    private sealed class TemplateGridRow(QuickOrderTemplateDto dto)
    {
        public Guid TemplateId => dto.TemplateId;
        public string Name => dto.Name;
        public string Description => dto.Description ?? "-";
        public int DisplayOrder => dto.DisplayOrder;
        public string StatusText => dto.IsActive ? "Active" : "Inactive";
        public int ItemCount => dto.Items.Count;
        public decimal TotalPrice => dto.TotalPrice;
        public string TotalDisplay => CurrencyDisplay.FormatPlain(dto.TotalPrice);
    }
}
