using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.MasterData.Application.FiscalYears.Commands;
using Clovent.MasterData.Application.FiscalYears.Dtos;
using Clovent.MasterData.Application.FiscalYears.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.MasterData.FiscalYears;

/// <summary>
/// Fiscal Year Management screen: search, filter, CRUD, and close over the
/// fiscal years belonging to a selected organization. There is no "activate"
/// concept - closing a fiscal year is one-way (see <c>FiscalYear.Close</c>),
/// so this screen only ever wires <see cref="MasterDataListView{TDto}.OnDeactivate"/>
/// (relabelled "Close") and leaves <c>OnActivate</c> unset. Feature-gated
/// per <c>fiscalyears.{create|edit|deactivate}</c>.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class FiscalYearManagementView : XtraUserControl
{
    private const string FeatureCode = "fiscalyears";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public FiscalYearManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private async Task<IReadOnlyList<FiscalYearDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        if (_selector.SelectedOrganizationId is not { } organizationId)
        {
            return [];
        }

        var items = await _mediator.Send(new ListFiscalYearsByOrganizationQuery(organizationId), cancellationToken);
        return [.. items];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task CreateAsync()
    {
        if (_selector.SelectedOrganizationId is not { } organizationId)
        {
            XtraMessageBox.Show(this, "Select an organization first.", "No Organization Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new FiscalYearEditForm("New Fiscal Year");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateFiscalYearCommand(organizationId, form.FiscalYearNameValue, form.StartDate, form.EndDate));
        }
    }

    private async Task EditAsync(FiscalYearDto dto)
    {
        using var form = new FiscalYearEditForm("Edit Fiscal Year", dto.Name, dto.StartDate, dto.EndDate, isNew: false);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new RenameFiscalYearCommand(dto.FiscalYearId, form.FiscalYearNameValue));
        }
    }

    private async void Selector_SelectionChanged(object? sender, EventArgs e) => await _listView.RefreshAsync();

    private async void FiscalYearManagementView_Load(object? sender, EventArgs e) => await _selector.LoadOrganizationsAsync();
}
