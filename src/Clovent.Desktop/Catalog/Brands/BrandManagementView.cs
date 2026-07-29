using Clovent.Catalog.Application.Brands.Commands;
using Clovent.Catalog.Application.Brands.Dtos;
using Clovent.Catalog.Application.Brands.Queries;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Catalog.Brands;

/// <summary>
/// Brand Management screen: search, filter, CRUD, activate/deactivate over
/// the shared brand catalog. Feature-gated per
/// <c>brands.{create|edit|activate|deactivate}</c>.
/// </summary>
public sealed class BrandManagementView : XtraUserControl
{
    private const string FeatureCode = "brands";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly MasterDataListView<BrandDto> _listView;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public BrandManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<BrandDto>(
        [
            new MasterDataColumn(nameof(BrandDto.Name), "Name", 220),
            new MasterDataColumn(nameof(BrandDto.Status), "Status", 90),
            new MasterDataColumn(nameof(BrandDto.CreatedAtUtc), "Created (UTC)", 160),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => dto.Name,
            StatusSelector = dto => dto.Status,
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnEdit = EditAsync,
            OnActivate = dto => _mediator.Send(new ActivateBrandCommand(dto.BrandId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateBrandCommand(dto.BrandId)),
        };

        Controls.Add(_listView);
        Load += async (_, _) => await _listView.RefreshAsync();
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

    private async Task<IReadOnlyList<BrandDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        var items = await _mediator.Send(new ListBrandsQuery(), cancellationToken);
        return [.. items];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task CreateAsync()
    {
        using var form = new BrandEditForm("New Brand");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateBrandCommand(form.NameValue));
        }
    }

    private async Task EditAsync(BrandDto dto)
    {
        using var form = new BrandEditForm("Edit Brand", dto.Name);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new RenameBrandCommand(dto.BrandId, form.NameValue));
        }
    }
}
