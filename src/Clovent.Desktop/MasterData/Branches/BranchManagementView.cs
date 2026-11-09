using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Identity.Application.Branches.Commands;
using Clovent.Identity.Application.Branches.Dtos;
using Clovent.Identity.Application.Branches.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.MasterData.Branches;

/// <summary>
/// Branch Management screen: search, filter, CRUD, activate/deactivate over
/// the branches belonging to a selected company (itself under a selected
/// organization). Feature-gated per <c>branches.{create|edit|activate|deactivate}</c>.
/// Control tree lives in <c>BranchManagementView.Designer.cs</c>; this file
/// holds behavior only.
/// </summary>
[System.ComponentModel.DesignerCategory("UserControl")]
public sealed partial class BranchManagementView : XtraUserControl
{
    private const string FeatureCode = "branches";

    private readonly IServiceScope? _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private bool _resourcesDisposed;

    /// <summary>Constructs the real controls without resolving services or loading data.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public BranchManagementView()
    {
        _mediator = null!;
        _featurePolicy = null!;
        _currentSession = null!;
        InitializeComponent();
    }

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public BranchManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(currentSession);
        InitializeComponent();
        IServiceScope? scope = null;
        try
        {
            scope = scopeFactory.CreateScope();
            _mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            _featurePolicy = scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
            _currentSession = currentSession;
            _selector.BindMediator(_mediator);
            _listView.LoadItemsAsync = LoadItemsAsync;
            _listView.SearchTextSelector = dto => dto.Name;
            _listView.StatusSelector = dto => dto.Status;
            _listView.CanUseFeatureAsync = CanUseFeatureAsync;
            _listView.OnNew = CreateAsync;
            _listView.OnEdit = EditAsync;
            _listView.OnActivate = dto => _mediator.Send(new ActivateBranchCommand(dto.BranchId));
            _listView.OnDeactivate = dto => _mediator.Send(new DeactivateBranchCommand(dto.BranchId));
            _scope = scope;
        }
        catch
        {
            Dispose();
            scope?.Dispose();
            throw;
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_resourcesDisposed)
        {
            _resourcesDisposed = true;
            _listView?.Dispose();
            _selector?.Dispose();
            _scope?.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private async void Selector_SelectionChanged(object? sender, EventArgs e)
    {
        if (_resourcesDisposed || _scope is null || Forms.Base.DesignModeHelper.IsInDesignMode) return;
        await _listView.RefreshAsync();
    }

    private async void BranchManagementView_Load(object? sender, EventArgs e)
    {
        if (_resourcesDisposed || _scope is null || Forms.Base.DesignModeHelper.IsInDesignMode) return;
        await _selector.LoadOrganizationsAsync();
    }

    private async Task<IReadOnlyList<BranchDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        if (_resourcesDisposed || _scope is null || _selector.SelectedCompanyId is not { } companyId)
        {
            return [];
        }

        var items = await _mediator.Send(new ListBranchesByCompanyQuery(companyId), cancellationToken);
        return [.. items];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        !_resourcesDisposed && _scope is not null && _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task CreateAsync()
    {
        if (_selector.SelectedCompanyId is not { } companyId)
        {
            XtraMessageBox.Show(this, "Select a company first.", "No Company Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new BranchEditForm("New Branch");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateBranchCommand(
                companyId, form.BranchNameValue, form.Street, form.City, form.State, form.PostalCode, form.Country));
        }
    }

    private async Task EditAsync(BranchDto dto)
    {
        using var form = new BranchEditForm("Edit Branch", dto.Name, dto.Street, dto.City, dto.State, dto.PostalCode, dto.Country);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new RenameBranchCommand(dto.BranchId, form.BranchNameValue));
            await _mediator.Send(new SetBranchAddressCommand(dto.BranchId, form.Street, form.City, form.State, form.PostalCode, form.Country));
        }
    }
}
