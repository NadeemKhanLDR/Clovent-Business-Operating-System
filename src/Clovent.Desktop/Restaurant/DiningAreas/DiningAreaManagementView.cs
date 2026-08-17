using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.DiningAreas.Commands;
using Clovent.Restaurant.Application.DiningAreas.Dtos;
using Clovent.Restaurant.Application.DiningAreas.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Restaurant.DiningAreas;

/// <summary>
/// Dining Area Management screen: search, filter, CRUD, activate/deactivate
/// over the dining areas belonging to a selected branch - the parent every
/// <see cref="Clovent.Restaurant.Tables.Table"/> is grouped under (e.g.
/// "Main Hall", "Patio", "Bar"). Feature-gated per
/// <c>diningareas.{create|edit|activate|deactivate}</c>.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class DiningAreaManagementView : XtraUserControl
{
    private const string FeatureCode = "diningareas";

    private readonly IServiceScope _scope;
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public DiningAreaManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        // Serialized so this screen's own event handlers/notifications can
        // never race a button click on this scope's DbContext - see
        // SerializedMediator's own doc comment for the exact failure mode
        // this prevents.
        _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
        _featurePolicy = new SerializedFeatureAuthorizationPolicy(_scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>(), _gate);
        _currentSession = currentSession;

        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _scope.Dispose();
            _gate.Dispose();
        }

        base.Dispose(disposing);
    }

    private async void DiningAreaManagementView_Load(object? sender, EventArgs e) => await _selector.LoadOrganizationsAsync();

    private async void Selector_SelectionChanged(object? sender, EventArgs e) => await _listView.RefreshAsync();

    private async Task<IReadOnlyList<DiningAreaDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        if (_selector.SelectedBranchId is not { } branchId)
        {
            return [];
        }

        var items = await _mediator.Send(new ListDiningAreasByBranchQuery(branchId), cancellationToken);
        return [.. items];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task CreateAsync()
    {
        if (_selector.SelectedBranchId is not { } branchId)
        {
            XtraMessageBox.Show(this, "Select a branch first.", "No Branch Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new DiningAreaEditForm("New Dining Area");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateDiningAreaCommand(branchId, form.NameValue));
        }
    }

    private async Task EditAsync(DiningAreaDto dto)
    {
        using var form = new DiningAreaEditForm("Edit Dining Area", dto.Name);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new RenameDiningAreaCommand(dto.DiningAreaId, form.NameValue));
        }
    }
}
