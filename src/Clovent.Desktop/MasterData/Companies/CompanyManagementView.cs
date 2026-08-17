using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Identity.Application.Companies.Commands;
using Clovent.Identity.Application.Companies.Dtos;
using Clovent.Identity.Application.Companies.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.MasterData.Companies;

/// <summary>
/// Company Management screen: search, filter, CRUD, activate/deactivate
/// over the companies belonging to a selected organization. Built on the
/// shared <see cref="MasterDataListView{TDto}"/> chrome plus
/// <see cref="OrganizationHierarchySelector"/> for the parent picker.
/// Feature-gated per <c>companies.{create|edit|activate|deactivate}</c>.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class CompanyManagementView : XtraUserControl
{
    private const string FeatureCode = "companies";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public CompanyManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
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

    private async Task<IReadOnlyList<CompanyDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        if (_selector.SelectedOrganizationId is not { } organizationId)
        {
            return [];
        }

        var items = await _mediator.Send(new ListCompaniesByOrganizationQuery(organizationId), cancellationToken);
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

        using var form = new CompanyEditForm("New Company");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateCompanyCommand(organizationId, form.CompanyNameValue, form.TaxId));
        }
    }

    private async Task EditAsync(CompanyDto dto)
    {
        using var form = new CompanyEditForm("Edit Company", dto.Name, dto.TaxId);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new RenameCompanyCommand(dto.CompanyId, form.CompanyNameValue));
            await _mediator.Send(new SetCompanyTaxIdCommand(dto.CompanyId, form.TaxId));
        }
    }

    private async void Selector_SelectionChanged(object? sender, EventArgs e) => await _listView.RefreshAsync();

    private async void CompanyManagementView_Load(object? sender, EventArgs e) => await _selector.LoadOrganizationsAsync();
}
