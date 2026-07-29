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
public sealed class CompanyManagementView : XtraUserControl
{
    private const string FeatureCode = "companies";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly OrganizationHierarchySelector _selector;
    private readonly MasterDataListView<CompanyDto> _listView;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public CompanyManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<CompanyDto>(
        [
            new MasterDataColumn(nameof(CompanyDto.Name), "Name", 220),
            new MasterDataColumn(nameof(CompanyDto.TaxId), "Tax Id", 140),
            new MasterDataColumn(nameof(CompanyDto.Status), "Status", 90),
            new MasterDataColumn(nameof(CompanyDto.CreatedAtUtc), "Created (UTC)", 160),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => dto.Name,
            StatusSelector = dto => dto.Status,
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnEdit = EditAsync,
            OnActivate = dto => _mediator.Send(new ActivateCompanyCommand(dto.CompanyId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateCompanyCommand(dto.CompanyId)),
        };

        _selector = new OrganizationHierarchySelector(_mediator, showCompany: false, showBranch: false);
        _selector.SelectionChanged += async (_, _) => await _listView.RefreshAsync();

        Controls.Add(_listView);
        Controls.Add(_selector);
        Load += async (_, _) => await _selector.LoadOrganizationsAsync();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope.Dispose();
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
}
