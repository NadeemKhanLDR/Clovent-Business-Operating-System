using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Identity.Application.Organizations.Commands;
using Clovent.Identity.Application.Organizations.Dtos;
using Clovent.Identity.Application.Organizations.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.MasterData.Organizations;

/// <summary>
/// Organization Management screen: search, filter, CRUD, activate/deactivate
/// over every organization in the system - the top of the tenant hierarchy.
/// Built entirely on the shared <see cref="MasterDataListView{TDto}"/>
/// chrome. Feature-gated per <c>organizations.{create|edit|activate|deactivate}</c>.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class OrganizationManagementView : XtraUserControl
{
    private const string FeatureCode = "organizations";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public OrganizationManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
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

    private async Task<IReadOnlyList<OrganizationDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        var items = await _mediator.Send(new ListOrganizationsQuery(), cancellationToken);
        return [.. items];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task CreateAsync()
    {
        using var form = new OrganizationEditForm("New Organization");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateOrganizationCommand(form.OrganizationName, form.TaxId));
        }
    }

    private async Task EditAsync(OrganizationDto dto)
    {
        using var form = new OrganizationEditForm("Edit Organization", dto.Name, dto.TaxId);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new RenameOrganizationCommand(dto.OrganizationId, form.OrganizationName));
            await _mediator.Send(new SetOrganizationTaxIdCommand(dto.OrganizationId, form.TaxId));
        }
    }

    private async void OrganizationManagementView_Load(object? sender, EventArgs e) => await _listView.RefreshAsync();
}
